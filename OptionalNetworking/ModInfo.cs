using Player;
using SNetwork;
using System;
using System.Collections.Generic;

namespace OptionalNetworking
{
    /// <summary> Wrapper class for registered mods. Contains callbacks and helpful collections.</summary>
    public class ModInfo
    {
        /// <summary> The name this mod was registered with.</summary>
        public readonly string Name;
        internal readonly ulong Hash;
        /// <summary> The unique mod mask this mod was given. Used with PlayerInfo to check if a player has this mod.</summary>
        public readonly long Mask;
        /// <summary> Whether the master has this mod installed.</summary>
        public bool MasterHasMod { get; private set; }
        /// <summary> A collection of bots. Empty if Host does not have the mod.</summary>
        public IReadOnlyCollection<SNet_Player> Bots => _bots.Values;
        /// <summary> A collection of other players that have the mod.</summary>
        public IReadOnlyCollection<SNet_Player> OtherPlayers => _others.Values;
        /// <summary> A collection of all players, including bots, that have the mod.</summary>
        public IReadOnlyCollection<SNet_Player> Players => _players.Values;

        private ModInfo(string name, ulong hash, long mask)
        {
            Name = name;
            Hash = hash;
            Mask = mask;
        }

        /// <summary> Invoked when a player joins the lobby.</summary>
        public event Action<SNet_Player>? OnPlayerJoined;
        /// <summary> Invoked when a player's mod status is updated to installed or uninstalled, passed as the boolean. Only bots can be set to false, when the host migrates to a player without the mod.</summary>
        public event Action<SNet_Player, bool>? OnPlayerModSet;
        /// <summary> Invoked when a player leaves the lobby.</summary>
        public event Action<SNet_Player>? OnPlayerLeft;
        /// <summary> Invoked when a player agent spawns.</summary>
        public event Action<PlayerAgent>? OnPlayerSpawned;
        /// <summary> Invoked when a player agent despawns.</summary>
        public event Action<PlayerAgent>? OnPlayerDespawned;
        /// <summary> Invoked when the master's mod status changes. Passes whether they have the mod installed as the boolean.</summary>
        public event Action<bool>? OnMasterModSet;
        /// <summary> Invoked after leaving the lobby.</summary>
        public event Action? OnLobbyLeft;
        private readonly List<ICustomDataSender> _customDataSenders = new();
        private readonly List<ICustomDataManager> _customDataManagers = new();

        private readonly Dictionary<ulong, SNet_Player> _bots = new(3);
        private readonly Dictionary<ulong, SNet_Player> _others = new(3);
        private readonly Dictionary<ulong, SNet_Player> _players = new(4);

        /// <summary>
        /// Adds a helper that can send data packets to other players with the mod installed.
        /// </summary>
        /// <param name="receiveEvent">The function to execute when receiving data for the given player.</param>
        /// <param name="defaultProvider">An optional function that returns default data for the local player (and bots, if host) to be sent to other players on initial connection.</param>
        /// <param name="mode">Determines which players this helper sends data to.</param>
        public CustomDataSender<T> AddCustomDataSender<T>(Action<SNet_Player, T> receiveEvent, Func<SNet_Player, T>? defaultProvider = null, SenderMode mode = SenderMode.OtherPlayers) where T : unmanaged
        {
            var sender = CustomDataSender<T>.CreateDataSender(this, _customDataSenders.Count, receiveEvent, defaultProvider, mode);
            _customDataSenders.Add(sender);
            return sender;
        }

        /// <summary>
        /// Adds a helper that tracks custom data for all players. Data updates are sent locally and to other players.
        /// </summary>
        /// <param name="defaultProvider">A function that returns default data for the local player (and bots, if host) to be sent locally and to other players on initial connection.</param>
        public CustomDataManager<T> AddCustomDataManager<T>(Func<SNet_Player, T> defaultProvider) where T : unmanaged
        {
            var manager = CustomDataManager<T>.CreateDataManager(this, _customDataSenders.Count, defaultProvider);
            _customDataSenders.Add(manager);
            _customDataManagers.Add(manager);
            return manager;
        }

        internal void InvokePlayerJoined(PlayerInfo info)
        {
            OnPlayerJoined?.Invoke(info.Owner);
        }

        internal void InvokePlayerModSet(PlayerInfo info)
        {
            bool hasMod = info.HasMod(Mask);
            if (hasMod && !_players.TryAdd(info.Lookup, info.Owner)) return;
            else if (!hasMod && !_players.Remove(info.Lookup)) return;

            if (!info.IsLocal)
            {
                var playerDict = info.IsBot ? _bots : _others;
                if (hasMod)
                    playerDict.Add(info.Lookup, info.Owner);
                else
                    playerDict.Remove(info.Lookup);
            }

            OnPlayerModSet?.Invoke(info.Owner, hasMod);
            if (hasMod)
            {
                foreach (var sender in _customDataSenders)
                    sender.SendDefaultData(info.Owner);
            }
            else
            {
                foreach (var manager in _customDataManagers)
                    manager.OnPlayerModRemoved(info.Owner);
            }
        }

        internal void InvokePlayerLeft(PlayerInfo info)
        {
            (info.IsBot ? _bots : _others).Remove(info.Lookup);
            OnPlayerLeft?.Invoke(info.Owner);
            foreach (var sender in _customDataSenders)
                sender.OnPlayerLeft(info.Owner);
        }

        internal void InvokePlayerSpawned(PlayerInfo info)
        {
            OnPlayerSpawned?.Invoke(info.Player!);
        }

        internal void InvokePlayerDespawned(PlayerInfo info)
        {
            OnPlayerDespawned?.Invoke(info.Player!);
        }

        internal void InvokeMasterSet(PlayerInfo info)
        {
            MasterHasMod = info.HasMod(Mask);
            OnMasterModSet?.Invoke(MasterHasMod);
        }

        internal void InvokeLobbyLeft()
        {
            _bots.Clear();
            _others.Clear();
            _players.Clear();
            MasterHasMod = false;
            OnLobbyLeft?.Invoke();
            foreach (var sender in _customDataSenders)
                sender.OnLobbyLeft();
        }

        private static long s_nextMask = 2;
        private static long GetNextMask()
        {
            // Out of capacity
            if (s_nextMask == 0) return 0;

            var mask = s_nextMask & ~1;
            if ((s_nextMask & 1) != 0)
            {
                if (mask == long.MinValue)
                {
                    s_nextMask = 0;
                    return mask | 1;
                }

                s_nextMask = (mask << 1) | 1;
                return mask | 1;
            }

            if (mask == long.MinValue)
            {
                s_nextMask = 2 | 1;
                return mask;
            }

            s_nextMask = mask << 1;
            return mask;
        }

        internal static ModInfo CreateModInfo(string name, ulong hash) => new(name, hash, GetNextMask());
    }
}
