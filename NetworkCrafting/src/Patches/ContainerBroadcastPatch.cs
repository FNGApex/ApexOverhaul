using HarmonyLib;
using System.Runtime.CompilerServices;

/// <summary>
/// Thin helper so other files don't need to import the ConditionalWeakTable directly.
/// </summary>
public static class ContainerBroadcastHelper
{
    private static readonly ConditionalWeakTable<TileEntityLootContainer, BroadcastState> states
        = new ConditionalWeakTable<TileEntityLootContainer, BroadcastState>();

    public static bool IsBroadcasting(TileEntityLootContainer te)
        => states.GetOrCreateValue(te).IsBroadcasting;

    public static void SetBroadcasting(TileEntityLootContainer te, bool value)
        => states.GetOrCreateValue(te).IsBroadcasting = value;

    private class BroadcastState { public bool IsBroadcasting = true; }
}

// -----------------------------------------------------------------------
// Persist isBroadcasting through save/load
// -----------------------------------------------------------------------

[HarmonyPatch(typeof(TileEntityLootContainer), "read")]
public static class Patch_TileEntityLootContainer_Read
{
    // Postfix: after the game reads its own data, read our extra byte
    [HarmonyPostfix]
    public static void Postfix(TileEntityLootContainer __instance, PooledBinaryReader _br)
    {
        try
        {
            // Guard: only read if data is still available
            if (_br.BaseStream.Position < _br.BaseStream.Length)
            {
                bool isBroadcasting = _br.ReadBoolean();
                ContainerBroadcastHelper.SetBroadcasting(__instance, isBroadcasting);
            }
            else
            {
                // New container — default to true
                ContainerBroadcastHelper.SetBroadcasting(__instance, true);
            }
        }
        catch
        {
            ContainerBroadcastHelper.SetBroadcasting(__instance, true);
        }

        // Register (or re-register) with the network after loading
        ContainerNetworkManager.Instance.RegisterContainer(__instance);
    }
}

[HarmonyPatch(typeof(TileEntityLootContainer), "write")]
public static class Patch_TileEntityLootContainer_Write
{
    [HarmonyPostfix]
    public static void Postfix(TileEntityLootContainer __instance, PooledBinaryWriter _bw)
    {
        _bw.Write(ContainerBroadcastHelper.IsBroadcasting(__instance));
    }
}

// -----------------------------------------------------------------------
// Display name: prepend "(Broadcasting)" when active
// -----------------------------------------------------------------------

[HarmonyPatch(typeof(TileEntityLootContainer), "GetCustomName")]
public static class Patch_TileEntityLootContainer_GetCustomName
{
    [HarmonyPostfix]
    public static void Postfix(TileEntityLootContainer __instance, ref string __result)
    {
        if (ContainerBroadcastHelper.IsBroadcasting(__instance))
            __result = "(Broadcasting) " + __result;
    }
}
