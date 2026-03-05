using HarmonyLib;
using System;
using System.Collections.Generic;

/// <summary>
/// Hooks XUiM_PlayerInventory so all crafting material checks and consumption
/// include items from the player's broadcasting network containers.
///
/// Strategy:
///   • Postfix HasItems — if player alone is short, check player + network combined.
///   • Prefix RemoveItems — record what network must cover (the player deficit).
///   • Postfix RemoveItems — consume that recorded deficit from network containers.
/// </summary>

// ── HasItems: pass when player + network together cover every requirement ─────

[HarmonyPatch(typeof(XUiM_PlayerInventory), "HasItems")]
public static class Patch_XUiM_PlayerInventory_HasItems
{
    // Guard: HasItems is called inside RemoveItems; prevent infinite recursion.
    [ThreadStatic] private static bool _inCheck;

    [HarmonyPostfix]
    public static void Postfix(XUiM_PlayerInventory __instance,
                               IList<ItemStack> _itemStacks, int _multiplier,
                               ref bool __result)
    {
        if (__result || _inCheck) return;

        EntityPlayer player = __instance.localPlayer;
        if (player == null) return;
        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(player)) return;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return;

        var netCounts = BuildNetworkCounts(player);
        if (netCounts == null) return;

        for (int i = 0; i < _itemStacks.Count; i++)
        {
            int needed   = _itemStacks[i].count * _multiplier;
            int inPlayer = __instance.backpack.GetItemCount(_itemStacks[i].itemValue)
                         + __instance.toolbelt.GetItemCount(_itemStacks[i].itemValue);
            int deficit  = needed - inPlayer;
            if (deficit <= 0) continue;

            netCounts.TryGetValue(_itemStacks[i].itemValue.type, out int inNet);
            if (inNet < deficit) return; // network cannot cover this item
        }

        __result = true;
    }

    /// <summary>
    /// Builds a type → total-count map from the player's network inventory.
    /// Returns null when the network is empty or unreachable.
    /// Sets _inCheck while running so nested HasItems calls skip our postfix.
    /// </summary>
    internal static Dictionary<int, int> BuildNetworkCounts(EntityPlayer player)
    {
        _inCheck = true;
        try
        {
            var stacks = ContainerNetworkManager.Instance.GetNetworkInventory(player);
            if (stacks.Count == 0) return null;

            var counts = new Dictionary<int, int>();
            foreach (var s in stacks)
            {
                if (s == null || s.IsEmpty()) continue;
                int t = s.itemValue.type;
                counts[t] = counts.TryGetValue(t, out int n) ? n + s.count : s.count;
            }
            return counts;
        }
        finally { _inCheck = false; }
    }
}

// ── RemoveItems: consume network deficit after player portion is taken ─────────

[HarmonyPatch(typeof(XUiM_PlayerInventory), "RemoveItems")]
public static class Patch_XUiM_PlayerInventory_RemoveItems
{
    [ThreadStatic] private static ItemStack[] _deficit;
    [ThreadStatic] private static EntityPlayer _deficitPlayer;

    [HarmonyPrefix]
    public static void Prefix(XUiM_PlayerInventory __instance,
                               IList<ItemStack> _itemStacks, int _multiplier)
    {
        _deficit       = null;
        _deficitPlayer = null;

        EntityPlayer player = __instance.localPlayer;
        if (player == null) return;
        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(player)) return;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return;

        var netCounts = Patch_XUiM_PlayerInventory_HasItems.BuildNetworkCounts(player);
        if (netCounts == null) return;

        var deficit = new List<ItemStack>();
        for (int i = 0; i < _itemStacks.Count; i++)
        {
            int needed    = _itemStacks[i].count * _multiplier;
            int inPlayer  = __instance.backpack.GetItemCount(_itemStacks[i].itemValue)
                          + __instance.toolbelt.GetItemCount(_itemStacks[i].itemValue);
            int shortfall = needed - inPlayer;
            if (shortfall <= 0) continue;

            netCounts.TryGetValue(_itemStacks[i].itemValue.type, out int inNet);
            if (inNet < shortfall) return; // network can't cover — bail, let vanilla handle failure

            deficit.Add(new ItemStack(_itemStacks[i].itemValue.Clone(), shortfall));
        }

        if (deficit.Count > 0)
        {
            _deficit       = deficit.ToArray();
            _deficitPlayer = player;
        }
    }

    // Runs even when the original returns early (e.g. !HasItems) — safe because
    // _deficit is only set when we are certain the network can cover the shortfall.
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (_deficit != null && _deficitPlayer != null)
        {
            ContainerNetworkManager.Instance.ConsumeFromNetwork(_deficitPlayer, _deficit);
            _deficit       = null;
            _deficitPlayer = null;
        }
    }
}
