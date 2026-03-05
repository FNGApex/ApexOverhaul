using HarmonyLib;
using UnityEngine;

/// <summary>
/// Applies / removes the "buffNetworkCraftingActive" status effect based on
/// whether the player is inside an owned LCB and blood moon is not active.
///
/// Uses a throttled tick (every ~2 seconds of game time) instead of every frame.
/// </summary>
[HarmonyPatch(typeof(EntityPlayer), "OnUpdateLive")]
public static class Patch_EntityPlayer_OnUpdateLive
{
    private const float CHECK_INTERVAL = 2f; // seconds

    // Per-entity last-check timestamp stored outside the class via instance ID
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

        bool shouldHaveBuff =
            !ContainerNetworkManager.Instance.IsBloodMoonActive() &&
            ContainerNetworkManager.Instance.IsPlayerInOwnedLCB(__instance);

        bool hasBuff = __instance.Buffs.HasBuff("buffNetworkCraftingActive");

        if (shouldHaveBuff && !hasBuff)
            __instance.Buffs.AddBuff("buffNetworkCraftingActive");
        else if (!shouldHaveBuff && hasBuff)
            __instance.Buffs.RemoveBuff("buffNetworkCraftingActive");
    }
}
