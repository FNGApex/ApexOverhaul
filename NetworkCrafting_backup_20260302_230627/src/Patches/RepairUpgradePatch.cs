using HarmonyLib;
using System.Collections.Generic;

/// <summary>
/// Repair: allow network containers to supply missing materials.
/// Upgrade: explicitly excluded — the isUpgradeItem guard on every patch
///          ensures the network is never consulted for block upgrades.
///
/// Patched methods (all on ItemActionRepair):
///   Item repair  → canRemoveRequiredItem / removeRequiredItem
///   Block repair → CanRemoveRequiredResource  (optimistic check)
///
/// Note on CanRemoveRequiredResource:
///   The check is optimistic — it returns true when the network has any items.
///   The actual deficit is consumed by removeRequiredItem (called internally
///   by RemoveRequiredResource). Free-repair is possible only if the network
///   does not actually contain the required block resources; this edge case
///   is acceptable for v1 and can be tightened with a Transpiler later.
/// </summary>

// ── canRemoveRequiredItem: allow if network covers the shortfall ───────────────

[HarmonyPatch(typeof(ItemActionRepair), "canRemoveRequiredItem")]
public static class Patch_ItemActionRepair_canRemoveRequiredItem
{
    [HarmonyPostfix]
    public static void Postfix(ItemActionRepair __instance,
                               ItemInventoryData _data, ItemStack _itemStack,
                               ref bool __result)
    {
        if (__result || __instance.isUpgradeItem) return;

        EntityPlayer player = _data.holdingEntity as EntityPlayer;
        if (player == null) return;
        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(player)) return;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return;

        var netCounts = Patch_XUiM_PlayerInventory_HasItems.BuildNetworkCounts(player);
        if (netCounts == null) return;

        netCounts.TryGetValue(_itemStack.itemValue.type, out int inNet);
        if (inNet >= _itemStack.count)
            __result = true;
    }
}

// ── removeRequiredItem: consume from network when player is short ──────────────

[HarmonyPatch(typeof(ItemActionRepair), "removeRequiredItem")]
public static class Patch_ItemActionRepair_removeRequiredItem
{
    [HarmonyPostfix]
    public static void Postfix(ItemActionRepair __instance,
                               ItemInventoryData _data, ItemStack _itemStack,
                               ref bool __result)
    {
        if (__result || __instance.isUpgradeItem) return;

        EntityPlayer player = _data.holdingEntity as EntityPlayer;
        if (player == null) return;
        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(player)) return;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return;

        // Original failed to remove from player — try the full amount from network.
        var needed = new[] { new ItemStack(_itemStack.itemValue.Clone(), _itemStack.count) };
        if (ContainerNetworkManager.Instance.ConsumeFromNetwork(player, needed))
            __result = true;
    }
}

// ── CanRemoveRequiredResource: optimistic pass for block repair ───────────────

[HarmonyPatch(typeof(ItemActionRepair), "CanRemoveRequiredResource")]
public static class Patch_ItemActionRepair_CanRemoveRequiredResource
{
    [HarmonyPostfix]
    public static void Postfix(ItemActionRepair __instance,
                               ItemInventoryData data, BlockValue blockValue,
                               ref bool __result)
    {
        if (__result || __instance.isUpgradeItem) return;

        EntityPlayer player = data.holdingEntity as EntityPlayer;
        if (player == null) return;
        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(player)) return;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return;

        // Allow if network has any items — removeRequiredItem postfix consumes the deficit.
        var netCounts = Patch_XUiM_PlayerInventory_HasItems.BuildNetworkCounts(player);
        if (netCounts != null && netCounts.Count > 0)
            __result = true;
    }
}
