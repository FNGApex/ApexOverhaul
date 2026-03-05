using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(EntityPlayer), "OnUpdateLive")]
public static class Patch_EntityPlayer_OnUpdateLive
{
    private const float CHECK_INTERVAL = 5f;
    private const string BUFF_NAME = "buffNetworkCraftingActive";
    private const string TAG = "[NetworkCrafting]";

    private static readonly System.Collections.Generic.Dictionary<int, float> lastCheck
        = new System.Collections.Generic.Dictionary<int, float>();

    [HarmonyPostfix]
    public static void Postfix(EntityPlayer __instance)
    {
        if (__instance == null) return;

        int id = __instance.entityId;
        float now = Time.time;

        if (lastCheck.TryGetValue(id, out float last) && (now - last) < CHECK_INTERVAL)
            return;

        lastCheck[id] = now;

        Debug.Log($"{TAG} --- tick for entityId={id} ---");

        bool shouldHaveBuff = IsPlayerInOwnedLCB(__instance);
        bool hasBuff = __instance.Buffs.HasBuff(BUFF_NAME);

        Debug.Log($"{TAG} shouldHaveBuff={shouldHaveBuff}  hasBuff={hasBuff}");

        if (shouldHaveBuff && !hasBuff)
        {
            Debug.Log($"{TAG} Adding buff");
            __instance.Buffs.AddBuff(BUFF_NAME);
        }
        else if (!shouldHaveBuff && hasBuff)
        {
            Debug.Log($"{TAG} Removing buff");
            __instance.Buffs.RemoveBuff(BUFF_NAME);
        }
    }

    private static bool IsPlayerInOwnedLCB(EntityPlayer player)
    {
        PersistentPlayerList playerList = GameManager.Instance?.GetPersistentPlayerList();
        if (playerList == null)
        {
            Debug.Log($"{TAG} playerList is null");
            return false;
        }

        Debug.Log($"{TAG} m_lpBlockMap count={playerList.m_lpBlockMap.Count}");

        Vector3i playerBlockPos = new Vector3i(
            Mathf.FloorToInt(player.position.x),
            Mathf.FloorToInt(player.position.y),
            Mathf.FloorToInt(player.position.z));

        int lcbRadius = GamePrefs.GetInt(EnumGamePrefs.LandClaimSize) / 2;

        Debug.Log($"{TAG} playerPos={playerBlockPos}  lcbRadius={lcbRadius}");

        var localId = Platform.PlatformManager.InternalLocalUserIdentifier;
        Debug.Log($"{TAG} localId={localId}");

        foreach (var kvp in playerList.m_lpBlockMap)
        {
            Vector3i claimPos = kvp.Key;
            int dx = Mathf.Abs(playerBlockPos.x - claimPos.x);
            int dz = Mathf.Abs(playerBlockPos.z - claimPos.z);

            Debug.Log($"{TAG} LCB at {claimPos}  dx={dx}  dz={dz}  radius={lcbRadius}");

            if (dx > lcbRadius || dz > lcbRadius)
            {
                Debug.Log($"{TAG}   -> out of range, skipping");
                continue;
            }

            PersistentPlayerData data = kvp.Value;
            if (data == null)
            {
                Debug.Log($"{TAG}   -> data is null, skipping");
                continue;
            }

            Debug.Log($"{TAG}   -> in range. PrimaryId={data.PrimaryId}  ACL={data.ACL?.Count ?? 0} entries");

            bool canEdit =
                data.PrimaryId.Equals(localId) ||
                (data.ACL != null && data.ACL.Contains(localId));

            Debug.Log($"{TAG}   -> canEdit={canEdit}");

            if (canEdit) return true;
        }

        return false;
    }
}
