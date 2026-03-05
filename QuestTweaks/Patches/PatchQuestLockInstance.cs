using HarmonyLib;

namespace QuestTweaks.Patches
{
    /// <summary>
    /// QuestLockInstance.SetUnlocked() normally sets LockedOutUntil = WorldTime + 2000,
    /// preventing the POI from being quested again for ~2 in-game days.
    /// This patch sets LockedOutUntil to 0 so the lockout expires immediately.
    /// </summary>
    [HarmonyPatch(typeof(QuestLockInstance), nameof(QuestLockInstance.SetUnlocked))]
    public static class PatchQuestLockSetUnlocked
    {
        static void Postfix(QuestLockInstance __instance)
        {
            __instance.LockedOutUntil = 0uL;
        }
    }

    /// <summary>
    /// QuestLockInstance.CheckQuestLock() returns true when the lockout has expired
    /// (meaning the POI is available again). We force it to always return true
    /// when the POI is not actively locked by an in-progress quest.
    /// </summary>
    [HarmonyPatch(typeof(QuestLockInstance), nameof(QuestLockInstance.CheckQuestLock))]
    public static class PatchQuestLockCheck
    {
        static bool Prefix(QuestLockInstance __instance, ref bool __result)
        {
            // If the POI is actively locked (quest in progress), respect that
            if (__instance.IsLocked)
            {
                __result = false;
                return false;
            }

            // Otherwise, always available — skip the time check entirely
            __result = true;
            return false;
        }
    }
}
