using HarmonyLib;
using System.Collections.Generic;

/// <summary>
/// Adds a "Toggle Broadcasting" entry to the Hold-E radial/interact menu
/// for any loot container tile entity.
/// </summary>
[HarmonyPatch(typeof(XUiC_ItemActionList), "GetActions")]
public static class Patch_RadialMenu_GetActions
{
    [HarmonyPostfix]
    public static void Postfix(XUiC_ItemActionList __instance, List<ItemActionEntry> __result,
                               TileEntity ___tileEntity)
    {
        if (___tileEntity is TileEntityLootContainer lootTE)
        {
            __result.Add(new ItemActionEntry(
                label: Localization.Get("uiToggleBroadcasting"),
                action: () => ToggleBroadcasting(lootTE, __instance)));
        }
    }

    private static void ToggleBroadcasting(TileEntityLootContainer container,
                                            XUiC_ItemActionList actionList)
    {
        bool current = ContainerBroadcastHelper.IsBroadcasting(container);
        ContainerBroadcastHelper.SetBroadcasting(container, !current);
        ContainerNetworkManager.Instance.OnBroadcastingToggled(container);
        container.SetModified();

        // Refresh the menu so the display name updates immediately
        actionList.xui?.playerUI?.windowManager?.Close();
    }
}
