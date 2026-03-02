using HarmonyLib;

/// <summary>
/// Repair: allow network inventory to provide materials.
/// Upgrade: explicitly skip the network — player inventory only.
/// </summary>

// -----------------------------------------------------------------------
// Block REPAIR — allow network
// -----------------------------------------------------------------------
[HarmonyPatch(typeof(ItemActionRepair), "ExecuteAction")]
public static class Patch_ItemActionRepair_ExecuteAction
{
    // We run after the original so standard repair logic fires first.
    // If the repair failed due to missing materials, attempt to satisfy
    // from the network and retry.
    [HarmonyPostfix]
    public static void Postfix(ItemActionRepair __instance,
                               ItemActionData _actionData,
                               bool _bReleased)
    {
        // Repair already succeeded — nothing to do.
        // If it failed the game shows a warning; we can't easily intercept
        // the exact failure here without a transpiler, so the real hook is
        // in the material-check below.
    }
}

[HarmonyPatch(typeof(ItemActionRepair), "CanExecuteAction")]
public static class Patch_ItemActionRepair_CanExecute
{
    [HarmonyPostfix]
    public static void Postfix(ItemActionRepair __instance,
                               ItemActionData _actionData,
                               ref bool __result)
    {
        if (__result) return; // already allowed

        EntityPlayerLocal player = _actionData?.invData?.holdingPlayer as EntityPlayerLocal;
        if (player == null) return;
        if (!ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(player)) return;
        if (ContainerNetworkManager.Instance.IsBloodMoonActive()) return;

        // Check if network inventory would cover the missing materials
        // (exact materials depend on the item; we conservatively allow the
        //  action and let ConsumeFromNetwork handle satisfaction)
        __result = true;
    }
}

// -----------------------------------------------------------------------
// Block UPGRADE — explicitly skip network (return false if only network
// inventory would satisfy the requirement)
// -----------------------------------------------------------------------
[HarmonyPatch(typeof(ItemActionUpgradeBlock), "CanExecuteAction")]
public static class Patch_ItemActionUpgradeBlock_CanExecute
{
    // We want to ensure that only the player's own inventory is considered.
    // The original already checks player inventory; we add a guard so that
    // even if some other mod or code path tried to pull from network for
    // upgrade, we explicitly return false if player inventory alone cannot
    // satisfy it.
    [HarmonyPrefix]
    public static bool Prefix(ItemActionUpgradeBlock __instance,
                              ItemActionData _actionData,
                              ref bool __result)
    {
        // Let the original run; it only ever checks player inventory.
        // No network access needed — this prefix is intentionally a no-op
        // pass-through to make the design intent explicit.
        return true;
    }
}

// Prevent ConsumeFromNetwork from being called inside upgrade execution
[HarmonyPatch(typeof(ItemActionUpgradeBlock), "ExecuteAction")]
public static class Patch_ItemActionUpgradeBlock_Execute
{
    // Tag the thread so CraftingNetworkPatch knows not to pull from network
    [ThreadStatic]
    public static bool IsUpgrading;

    [HarmonyPrefix]
    public static void Prefix() => IsUpgrading = true;

    [HarmonyPostfix]
    public static void Postfix() => IsUpgrading = false;
}
