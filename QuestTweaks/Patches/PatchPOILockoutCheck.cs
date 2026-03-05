using HarmonyLib;
using UnityEngine;

namespace QuestTweaks.Patches
{
    /// <summary>
    /// QuestEventManager.CheckForPOILockouts() is the master lockout check called when
    /// the game selects POIs for quests. It checks for:
    ///   - QuestLock (active quest / post-quest timer)
    ///   - PlayerInside (another non-party player is in the POI)
    ///   - Bedroll (player home)
    ///   - LandClaim (player LCB)
    ///
    /// We only remove the QuestLock check. Bedroll, LandClaim, and PlayerInside
    /// protections remain intact — those are safety features, not cooldowns.
    /// </summary>
    [HarmonyPatch(typeof(QuestEventManager), nameof(QuestEventManager.CheckForPOILockouts))]
    public static class PatchCheckForPOILockouts
    {
        static bool Prefix(int entityId, Vector2 prefabPos, out ulong extraData, ref QuestEventManager.POILockoutReasonTypes __result)
        {
            extraData = 0uL;
            World world = GameManager.Instance.World;
            PrefabInstance prefabFromWorldPos = GameManager.Instance.GetDynamicPrefabDecorator()
                .GetPrefabFromWorldPos((int)prefabPos.x, (int)prefabPos.y);

            if (prefabFromWorldPos == null)
            {
                __result = QuestEventManager.POILockoutReasonTypes.None;
                return false;
            }

            // Clear any expired lock instances, but also clear the timer-based lockouts
            if (prefabFromWorldPos.lockInstance != null)
            {
                // Only respect active quest locks (someone is currently doing a quest there)
                if (!prefabFromWorldPos.lockInstance.IsLocked)
                {
                    // Post-quest timer lockout — remove it
                    prefabFromWorldPos.lockInstance = null;
                }
                else
                {
                    // Active quest in progress — still locked
                    extraData = prefabFromWorldPos.lockInstance.LockedOutUntil;
                    __result = QuestEventManager.POILockoutReasonTypes.QuestLock;
                    return false;
                }
            }

            // Check for players inside (non-party members)
            EntityPlayer entityPlayer = (EntityPlayer)world.GetEntity(entityId);
            Rect rect = new Rect(
                prefabFromWorldPos.boundingBoxPosition.x,
                prefabFromWorldPos.boundingBoxPosition.z,
                prefabFromWorldPos.boundingBoxSize.x,
                prefabFromWorldPos.boundingBoxSize.z);

            if (entityPlayer != null)
            {
                for (int i = 0; i < world.Players.list.Count; i++)
                {
                    EntityPlayer other = world.Players.list[i];
                    if (entityPlayer != other
                        && (!entityPlayer.IsInParty() || !entityPlayer.Party.MemberList.Contains(other))
                        && rect.Contains(new Vector2(other.position.x, other.position.z)))
                    {
                        __result = QuestEventManager.POILockoutReasonTypes.PlayerInside;
                        return false;
                    }
                }
            }

            // Check for bedroll / land claim — these are player protections, keep them
            __result = prefabFromWorldPos.CheckForAnyPlayerHome(world) switch
            {
                GameUtils.EPlayerHomeType.Bedroll => QuestEventManager.POILockoutReasonTypes.Bedroll,
                GameUtils.EPlayerHomeType.Landclaim => QuestEventManager.POILockoutReasonTypes.LandClaim,
                _ => QuestEventManager.POILockoutReasonTypes.None,
            };
            return false;
        }
    }
}
