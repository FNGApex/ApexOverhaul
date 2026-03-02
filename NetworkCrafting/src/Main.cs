using HarmonyLib;
using System;

public class NetworkCraftingMod : IModApi
{
    private static Harmony harmonyInstance;

    public void InitMod(Mod _modInstance)
    {
        harmonyInstance = new Harmony("com.networkcrafting.mod");
        harmonyInstance.PatchAll();

        ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
    }

    private static void OnGameStartDone(ref ModEvents.SGameStartDoneData _data)
    {
        ContainerNetworkManager.Instance.OnGameStarted();
    }

    private static void OnGameShutdown(ref ModEvents.SGameShutdownData _data)
    {
        ContainerNetworkManager.Instance.OnGameShutdown();
    }
}
