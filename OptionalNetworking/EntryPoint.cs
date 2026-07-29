using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using OptionalNetworking.Managers;

namespace OptionalNetworking
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.0.0")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "OptionalNetworkAPI";

        public override void Load()
        {
            ModSetNetworkManager.Init();
            new Harmony(MODNAME).PatchAll();
            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}