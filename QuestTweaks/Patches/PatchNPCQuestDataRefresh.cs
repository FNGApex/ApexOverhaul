using HarmonyLib;

namespace QuestTweaks.Patches
{
    /// <summary>
    /// NPCQuestData.PlayerQuestData caches the quest list and stores a LastUpdate timestamp
    /// rounded to the current in-game day. The game uses this to avoid regenerating quest
    /// lists within the same day.
    ///
    /// Previously this patch forced LastUpdate to 0, causing the trader to always regenerate
    /// fresh quests. This broke multi-quest acceptance — accepting one quest and re-opening
    /// the dialog would discard all remaining quests because the cache was always "expired."
    ///
    /// Now we let the vanilla setter keep its timestamp so the quest list persists within
    /// the same in-game day. The other QuestTweaks patches (PatchQuestTraderData,
    /// PatchQuestLockInstance, PatchPOILockoutCheck) still ensure all POIs are available
    /// when the cache expires and quests regenerate at the next day.
    /// </summary>
    [HarmonyPatch(typeof(NPCQuestData.PlayerQuestData), nameof(NPCQuestData.PlayerQuestData.QuestList), MethodType.Setter)]
    public static class PatchPlayerQuestDataLastUpdate
    {
        static void Postfix(NPCQuestData.PlayerQuestData __instance)
        {
            // No-op: let the vanilla setter keep LastUpdate = WorldTime / 24000 * 24000
            // so the quest list cache persists within the same in-game day,
            // allowing players to accept multiple quests per trader visit.
        }
    }
}
