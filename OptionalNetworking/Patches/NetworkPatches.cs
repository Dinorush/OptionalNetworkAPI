using HarmonyLib;
using OptionalNetworking.Managers;
using Player;
using SNetwork;

namespace OptionalNetworking.Patches
{
    [HarmonyPatch]
    internal static class NetworkPatches
    {
        [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.AddPlayerToSession))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_AddPlayer(SNet_Player player)
        {
            ModManager.OnAddPlayer(player);
            ModSetNetworkManager.OnAddPlayer(player);
        }

        [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.OnLeftLobby))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_RemovePlayer(SNet_Player player)
        {
            ModManager.OnRemovePlayer(player);
        }

        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnFoundMaster))]
        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnFoundNewMasterDuringMigration))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_FoundMaster()
        {
            ModManager.OnMasterSet();
        }

        [HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_PlayerSpawn(PlayerSync __instance)
        {
            ModManager.OnPlayerSpawned(__instance.m_agent);
        }

        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.OnDespawn))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlayerDespawn(PlayerAgent __instance)
        {
            ModManager.OnPlayerDespawned(__instance);
        }

        [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.LeaveHub))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_LeaveHub()
        {
            ModManager.OnLobbyLeft();
        }
    }
}
