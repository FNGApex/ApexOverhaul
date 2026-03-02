using HarmonyLib;

/// <summary>
/// Disables network crafting during blood moon (horde night).
/// When blood moon starts: network inventory returns empty, buff is removed.
/// When blood moon ends: buff is reapplied if player is still in LCB.
/// </summary>

// Detect transition to blood moon active
[HarmonyPatch(typeof(SkyManager), "BloodMoonSet")]
public static class Patch_SkyManager_BloodMoonSet
{
    [HarmonyPostfix]
    public static void Postfix(bool value)
    {
        if (GameManager.Instance?.World == null) return;

        foreach (var player in GameManager.Instance.World.Players.list)
        {
            if (player == null) continue;

            if (value)
            {
                // Blood moon starting — remove the buff
                if (player.Buffs.HasBuff("buffNetworkCraftingActive"))
                    player.Buffs.RemoveBuff("buffNetworkCraftingActive");
            }
            else
            {
                // Blood moon ending — reapply buff if still in LCB
                if (ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(player))
                {
                    if (!player.Buffs.HasBuff("buffNetworkCraftingActive"))
                        player.Buffs.AddBuff("buffNetworkCraftingActive");
                }
            }
        }
    }
}
