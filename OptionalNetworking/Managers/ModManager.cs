using OptionalNetworking.Hashing;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OptionalNetworking.Managers
{
    internal static class ModManager
    {
        private readonly static Dictionary<ulong, PlayerInfo> _bots = new();
        private readonly static Dictionary<ulong, PlayerInfo> _players = new();
        private readonly static Dictionary<string, ModInfo> _modInfos = new();
        private readonly static Dictionary<ulong, ModInfo> _hashToMod = new();
        private static (long mask1, long mask2) _localMask = (0, 1);

        public static PlayerInfo? MasterInfo { get; private set; }
        public static PlayerInfo? LocalInfo { get; private set; }
        public static IReadOnlyCollection<ModInfo> Mods => _modInfos.Values;
        public static bool HasMod(string name) => _modInfos.ContainsKey(name);
        public static bool HasMod(SNet_Player player, string name)
        {
            if (!_players.TryGetValue(player.Lookup, out var playerInfo) || !_modInfos.TryGetValue(name, out var modInfo)) return false;

            return playerInfo.HasMod(modInfo);
        }
        public static bool TryGetModInfo(string name, [MaybeNullWhen(false)] out ModInfo modInfo) => _modInfos.TryGetValue(name, out modInfo);
        public static bool TryGetPlayerInfo(SNet_Player player, [MaybeNullWhen(false)] out PlayerInfo playerInfo) => _players.TryGetValue(player.Lookup, out playerInfo);

        private static void CombineMask(ref (long mask1, long mask2) mask, long maskToAdd)
        {
            if ((maskToAdd & 1) == 0)
                mask.mask1 |= maskToAdd;
            else
                mask.mask2 |= maskToAdd;
        }

        internal static IReadOnlyCollection<ulong> ModHashes => _hashToMod.Keys;
        internal static (long mask1, long mask2) HashesToMask(ulong[] hashes)
        {
            (long, long) mask = (0, 1);
            foreach (ulong hash in hashes)
            {
                if (_hashToMod.TryGetValue(hash, out var modInfo))
                    CombineMask(ref mask, modInfo.Mask);
            }
            return mask;
        }

        internal static ModInfo AddMod(string name)
        {
            if (_modInfos.ContainsKey(name))
                throw new ArgumentException($"Cannot register duplicate mod name {name}.");

            ulong hash = MurmurHash2.Hash(name);
            if (_hashToMod.TryGetValue(hash, out var existingInfo))
                throw new ArgumentException($"Hash for mod name {name} collides with {existingInfo.Name}, can't register!");

            ModInfo info = ModInfo.CreateModInfo(name, hash);
            if (info.Mask == 0)
                throw new IndexOutOfRangeException($"Out of internal space (126 mods registered), unable to register mod {name}.");

            _modInfos.Add(name, info);
            _hashToMod.Add(hash, info);
            CombineMask(ref _localMask, info.Mask);
            return info;
        }

        internal static bool HasMod(SNet_Player? player, ModInfo modInfo)
        {
            if (player == null || !_players.TryGetValue(player.Lookup, out var playerInfo)) return false;

            return playerInfo.HasMod(modInfo);
        }

        internal static void OnAddPlayer(SNet_Player player)
        {
            if (_players.ContainsKey(player.Lookup)) return;

            PlayerInfo info = new(player);
            _players.Add(player.Lookup, info);
            foreach (var mod in _modInfos.Values)
                mod.InvokePlayerJoined(info);

            if (info.IsLocal)
            {
                LocalInfo = info;
                OnPlayerModSet(player, _localMask);
            }
            else if (info.IsBot)
            {
                _bots.Add(info.Lookup, info);
                if (MasterInfo != null)
                    OnPlayerModSet(player, MasterInfo.ModMask);
            }
        }

        internal static void OnRemovePlayer(SNet_Player player)
        {
            if (!_players.Remove(player.Lookup, out var info)) return;
            _bots.Remove(player.Lookup);

            if (info.IsLocal)
                LocalInfo = null;

            foreach (var mod in _modInfos.Values)
                mod.InvokePlayerLeft(info);
        }

        internal static void OnPlayerSpawned(PlayerAgent agent)
        {
            if (!_players.TryGetValue(agent.Owner.Lookup, out var info)) return;

            info.Player = agent;
            foreach (var mod in _modInfos.Values)
                mod.InvokePlayerSpawned(info);
        }

        internal static void OnPlayerDespawned(PlayerAgent agent)
        {
            if (!_players.TryGetValue(agent.Owner.Lookup, out var info) || info.Player == null) return;

            foreach (var mod in _modInfos.Values)
                mod.InvokePlayerDespawned(info);
            info.Player = null;
        }

        internal static void OnPlayerModSet(SNet_Player player, (long mask1, long mask2) modMask)
        {
            if (!_players.TryGetValue(player.Lookup, out var info))
            {
                OnAddPlayer(player);
                info = _players[player.Lookup];
            }

            if (!info.SetModMask(modMask)) return;

            foreach (var mod in _modInfos.Values)
                mod.InvokePlayerModSet(info);
        }

        internal static void OnLobbyLeft()
        {
            foreach (var mod in _modInfos.Values)
                mod.InvokeLobbyLeft();
            _players.Clear();
            MasterInfo = null;
            LocalInfo = null;
        }

        internal static void OnMasterSet()
        {
            if (!SNet.HasMaster)
            {
                MasterInfo = null;
                return;
            }

            if (!_players.TryGetValue(SNet.Master.Lookup, out var info))
            {
                OnAddPlayer(SNet.Master);
                info = _players[SNet.Master.Lookup];
            }
            else if (MasterInfo == info)
                return;

            MasterInfo = info;

            foreach (var mod in _modInfos.Values)
                mod.InvokeMasterSet(info);

            foreach (var bot in _bots.Values)
                OnPlayerModSet(bot.Owner, MasterInfo.ModMask);
        }
    }
}
