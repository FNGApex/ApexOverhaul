using HarmonyLib;

namespace QuestTweaks.Patches
{
    /// <summary>
    /// NPCQuestData.PlayerQuestData caches the quest list and stores a LastUpdate timestamp
    /// rounded to the current in-game day. The game uses this to avoid regenerating quest
    /// lists within the same day.
    ///
    /// This patch sets LastUpdate to 0 whenever the quest list is set, so the trader
    /// always regenerates fresh quests when the player talks to them.
    /// </summary>
    [HarmonyPatch(typeof(NPCQuestData.PlayerQuestData), nameof(NPCQuestData.PlayerQuestData.QuestList), MethodType.Setter)]
    public static class PatchPlayerQuestDataLastUpdate
    {
        static void Postfix(NPCQuestData.PlayerQuestData __instance)
        {
            __instance.LastUpdate = 0uL;
        }
    }
}
