using HarmonyLib;

namespace QuestTweaks.Patches
{
    /// <summary>
    /// QuestTraderData.CheckReset() clears completed POI lists for tiers 4-6 after 7 in-game days.
    /// Tiers 1-3 never reset by time — they only reset when all POIs of that tier are exhausted.
    ///
    /// This patch makes CheckReset() always clear all tiers immediately, so completed POI
    /// tracking never blocks quest availability.
    /// </summary>
    [HarmonyPatch(typeof(QuestTraderData), nameof(QuestTraderData.CheckReset))]
    public static class PatchCheckReset
    {
        static bool Prefix(QuestTraderData __instance)
        {
            // Clear all completed POI data for every tier
            __instance.CompletedPOIByTier.Clear();
            return false; // skip original
        }
    }

    /// <summary>
    /// QuestTraderData.AddPOI() records a completed POI position per tier.
    /// By skipping this entirely, no POIs are ever marked as "used" for a trader,
    /// so all POIs remain available for questing immediately.
    /// </summary>
    [HarmonyPatch(typeof(QuestTraderData), nameof(QuestTraderData.AddPOI))]
    public static class PatchAddPOI
    {
        static bool Prefix()
        {
            return false; // never record completed POIs
        }
    }
}
