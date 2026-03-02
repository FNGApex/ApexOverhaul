using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Merges network container inventory into the crafting resource pool so that
/// all crafting grids and workstations can see (and consume) network items.
/// </summary>

// -----------------------------------------------------------------------
// HasRequiredMaterials — allow crafting when network covers the gap
// -----------------------------------------------------------------------
[HarmonyPatch(typeof(RecipeCraftingUtils), "HasRequiredMaterials")]
public static class Patch_RecipeCraftingUtils_HasRequiredMaterials
{
    [HarmonyPrefix]
    public static bool Prefix(EntityPlayer _player, Recipe _recipe, int _count, ref bool __result)
    {
        if (_player == null || _recipe == null) return true; // run original

        // If original already passes, no need to intervene
        if (RecipeCraftingUtils.HasRequiredMaterialsOriginal(_player, _recipe, _count))
        {
            __result = true;
            return false;
        }

        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(_player)) return true;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return true;

        List<ItemStack> combined = BuildCombinedInventory(_player);
        __result = RecipeCraftingUtils.HasRequiredMaterials(_recipe, combined, _count);
        return false;
    }

    private static List<ItemStack> BuildCombinedInventory(EntityPlayer player)
    {
        var combined = new List<ItemStack>();

        // Player bag + toolbar
        if (player.bag?.items != null)
            foreach (var s in player.bag.items)
                if (s != null && !s.IsEmpty()) combined.Add(s.Clone());

        if (player.inventory?.holdingItemItemValue != null)
        { /* toolbar already covered by bag in 7D2D */ }

        // Network
        combined.AddRange(ContainerNetworkManager.Instance.GetNetworkInventory(player));
        return combined;
    }
}

// -----------------------------------------------------------------------
// RemoveMaterials — consume from player first, then network
// -----------------------------------------------------------------------
[HarmonyPatch(typeof(RecipeCraftingUtils), "RemoveMaterials")]
public static class Patch_RecipeCraftingUtils_RemoveMaterials
{
    [HarmonyPrefix]
    public static bool Prefix(EntityPlayer _player, Recipe _recipe, int _count)
    {
        if (_player == null || _recipe == null) return true;
        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(_player)) return true;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return true;

        // Calculate what remains after taking from player inventory
        ItemStack[] stillNeeded = ComputeRemainingAfterPlayer(_player, _recipe, _count);

        if (stillNeeded == null) return true; // player has everything; let original handle it

        // Pull the remainder from network containers
        if (stillNeeded.Length > 0)
            ContainerNetworkManager.Instance.ConsumeFromNetwork(_player, stillNeeded);

        // Let the original method run to consume the player-side portion
        return true;
    }

    private static ItemStack[] ComputeRemainingAfterPlayer(EntityPlayer player, Recipe recipe, int count)
    {
        if (recipe.ingredients == null) return null;

        var remaining = new List<ItemStack>();
        // Build a mutable copy of required counts
        var needed = new Dictionary<int, int>();
        foreach (var ing in recipe.ingredients)
        {
            if (ing == null || ing.IsEmpty()) continue;
            int type = ing.itemValue.type;
            if (needed.ContainsKey(type))
                needed[type] += ing.count * count;
            else
                needed[type] = ing.count * count;
        }

        // Subtract what the player has in their bag
        if (player.bag?.items != null)
        {
            foreach (var stack in player.bag.items)
            {
                if (stack == null || stack.IsEmpty()) continue;
                int type = stack.itemValue.type;
                if (needed.ContainsKey(type) && needed[type] > 0)
                {
                    int use = System.Math.Min(needed[type], stack.count);
                    needed[type] -= use;
                }
            }
        }

        foreach (var kvp in needed)
        {
            if (kvp.Value > 0)
            {
                remaining.Add(new ItemStack(
                    new ItemValue(kvp.Key, false),
                    kvp.Value));
            }
        }

        return remaining.Count == 0 ? null : remaining.ToArray();
    }
}
