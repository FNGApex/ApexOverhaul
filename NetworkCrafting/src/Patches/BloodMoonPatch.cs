using HarmonyLib;

/// <summary>
/// Ensures the network crafting buff is removed the moment blood moon becomes
/// active, without waiting for the PlayerBuffPatch 2-second polling cycle.
///
/// SkyManager.UpdateTimeOfDay() is called each frame during the day/night cycle.
/// We hook its postfix to detect the transition from "not blood moon" to "blood moon"
/// and immediately strip the buff from all local players.
/// </summary>
[HarmonyPatch(typeof(SkyManager), "UpdateTimeOfDay")]
public static class Patch_SkyManager_UpdateTimeOfDay
{
    private static bool wasBloodMoon = false;

    [HarmonyPostfix]
    public static void Postfix()
    {
        if (GameManager.Instance?.World == null) return;

        bool isNow = SkyManager.IsBloodMoonVisible();

        // Transition: normal → blood moon
        if (isNow && !wasBloodMoon)
        {
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                if (player != null && player.Buffs.HasBuff("buffNetworkCraftingActive"))
                    player.Buffs.RemoveBuff("buffNetworkCraftingActive");
            }
        }

        wasBloodMoon = isNow;
    }
}
