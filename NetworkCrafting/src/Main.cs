using HarmonyLib;
using System.Reflection;
using UnityEngine;

public class NetworkCraftingMod : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        Debug.Log("[NetworkCrafting] InitMod called");
        try
        {
            var harmony = new Harmony("com.networkcrafting.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("[NetworkCrafting] PatchAll completed successfully");
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[NetworkCrafting] PatchAll FAILED: {ex}");
        }
    }
}
