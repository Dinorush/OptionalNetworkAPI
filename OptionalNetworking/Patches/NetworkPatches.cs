using HarmonyLib;
using OptionalNetworking.Managers;
using OptionalNetworking.Networking;
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
            ModHandshakeHandler.OnAddPlayer(player);
        }

        [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.OnLeftLobby))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_RemovePlayer(SNet_Player player)
        {
            ModHandshakeHandler.OnRemovePlayer(player);
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
            ModHandshakeHandler.OnLobbyLeft();
            ModManager.OnLobbyLeft();
        }

        // This is the only location where LeaveHub does not get called, but LeaveLobby is ran.
        [HarmonyPatch(typeof(SNet_Lobby_STEAM), nameof(SNet_Lobby_STEAM.KeepLobbyAliveAndConnected))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_LobbyAlive(bool __result)
        {
            if (!__result)
                Post_LeaveHub();
        }
    }
}
