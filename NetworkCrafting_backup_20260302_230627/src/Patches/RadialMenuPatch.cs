using HarmonyLib;
using System;

/// <summary>
/// Injects a "Toggle Broadcasting" option into the Hold-E radial menu for
/// both regular (BlockLoot) and secure (BlockSecureLoot) loot containers.
///
/// Strategy:
///   • Postfix GetBlockActivationCommands — append our command to the returned array.
///   • Prefix OnBlockActivated(string) — intercept "togglebroadcasting" before
///     the base block processes it.
/// </summary>

// ── Regular (non-secure) loot containers ────────────────────────────────────

[HarmonyPatch(typeof(BlockLoot), "GetBlockActivationCommands")]
public static class Patch_BlockLoot_GetBlockActivationCommands
{
    [HarmonyPostfix]
    public static void Postfix(ref BlockActivationCommand[] __result,
                               WorldBase _world, int _clrIdx, Vector3i _blockPos)
    {
        __result = RadialMenuHelpers.AppendToggleCommand(__result, _world, _clrIdx, _blockPos);
    }
}

[HarmonyPatch(typeof(BlockLoot), "OnBlockActivated",
    new Type[] { typeof(string), typeof(WorldBase), typeof(int), typeof(Vector3i),
                 typeof(BlockValue), typeof(EntityPlayerLocal) })]
public static class Patch_BlockLoot_OnBlockActivated
{
    [HarmonyPrefix]
    public static bool Prefix(string _commandName, WorldBase _world,
                              int _cIdx, Vector3i _blockPos, ref bool __result)
    {
        return !RadialMenuHelpers.HandleToggleCommand(_commandName, _world, _cIdx, _blockPos, ref __result);
    }
}

// ── Secure loot containers ───────────────────────────────────────────────────

[HarmonyPatch(typeof(BlockSecureLoot), "GetBlockActivationCommands")]
public static class Patch_BlockSecureLoot_GetBlockActivationCommands
{
    [HarmonyPostfix]
    public static void Postfix(ref BlockActivationCommand[] __result,
                               WorldBase _world, int _clrIdx, Vector3i _blockPos)
    {
        __result = RadialMenuHelpers.AppendToggleCommand(__result, _world, _clrIdx, _blockPos);
    }
}

[HarmonyPatch(typeof(BlockSecureLoot), "OnBlockActivated",
    new Type[] { typeof(string), typeof(WorldBase), typeof(int), typeof(Vector3i),
                 typeof(BlockValue), typeof(EntityPlayerLocal) })]
public static class Patch_BlockSecureLoot_OnBlockActivated
{
    [HarmonyPrefix]
    public static bool Prefix(string _commandName, WorldBase _world,
                              int _cIdx, Vector3i _blockPos, ref bool __result)
    {
        return !RadialMenuHelpers.HandleToggleCommand(_commandName, _world, _cIdx, _blockPos, ref __result);
    }
}

// ── Shared helpers ───────────────────────────────────────────────────────────

internal static class RadialMenuHelpers
{
    public static BlockActivationCommand[] AppendToggleCommand(
        BlockActivationCommand[] original,
        WorldBase world, int clrIdx, Vector3i blockPos)
    {
        if (world.GetTileEntity(clrIdx, blockPos) is not TileEntityLootContainer te)
            return original;

        var extended = new BlockActivationCommand[original.Length + 1];
        original.CopyTo(extended, 0);
        extended[original.Length] = new BlockActivationCommand(
            "togglebroadcasting",
            "ui_game_symbol_container",
            _enabled: true,
            _highlighted: ContainerBroadcastHelper.IsBroadcasting(te));

        return extended;
    }

    public static bool HandleToggleCommand(string commandName, WorldBase world,
                                           int clrIdx, Vector3i blockPos,
                                           ref bool result)
    {
        if (commandName != "togglebroadcasting") return false;

        if (world.GetTileEntity(clrIdx, blockPos) is TileEntityLootContainer container)
        {
            bool newState = !ContainerBroadcastHelper.IsBroadcasting(container);
            ContainerBroadcastHelper.SetBroadcasting(container, newState);
            ContainerNetworkManager.Instance.OnBroadcastingToggled(container);
            container.SetModified();
        }

        result = true;
        return true;
    }
}
