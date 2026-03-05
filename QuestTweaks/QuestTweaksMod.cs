using System.Reflection;
using HarmonyLib;

public class QuestTweaksMod : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        var harmony = new Harmony("com.apexoverhaul.questtweaks");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Log.Out("[QuestTweaks] Harmony patches applied — all POI quest cooldowns removed.");
    }
}
