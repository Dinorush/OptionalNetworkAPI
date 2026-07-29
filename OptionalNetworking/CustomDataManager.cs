using SNetwork;
using System;
using System.Collections.Generic;

namespace OptionalNetworking
{
    internal interface ICustomDataManager : ICustomDataSender
    {
        void OnPlayerModRemoved(SNet_Player player);
    }
    
    /// <summary>
    /// Helper that manages custom data for all players.
    /// </summary>
    /// <typeparam name="T">The struct that will be stored and sent over the network.</typeparam>
    public class CustomDataManager<T> : CustomDataSender<T>, ICustomDataManager where T : unmanaged
    {
        private readonly Dictionary<ulong, T> _data = new();

        /// <summary> Callback invoked prior to data being set for the given player. Provides the new data.</summary>
        public event Action<SNet_Player, T>? PreDataSet;
        /// <summary> Callback invoked after the given player's data is set.</summary>
        public event Action<SNet_Player, T>? OnDataSet;
        /// <summary> Callback invoked after the given player's data is removed. Provides the old data.</summary>
        public event Action<SNet_Player, T>? OnDataRemoved;

        private CustomDataManager(ModInfo parent, string id, Func<SNet_Player, T> defaultProvider) : base(parent, id, defaultProvider, SenderMode.All)
        {
            ReceiveEvent = SetData;
        }

        internal static CustomDataManager<T> CreateDataManager(ModInfo parent, int index, Func<SNet_Player, T> defaultProvider)
        {
            return new CustomDataManager<T>(parent, $"{parent.Hash:x}_{index}", defaultProvider);
        }

        /// <summary> Gets the data associated with the given player.</summary>
        public bool TryGetData(SNet_Player player, out T data) => _data.TryGetValue(player.Lookup, out data);
        /// <summary> Gets or sets the data associated with the given player. If set, executes PreDataSet and OnDataSet callbacks.</summary>
        public T this[SNet_Player player]
        {
            get => _data[player.Lookup];
            set => SetData(player, value);
        }

        private void SetData(SNet_Player player, T data)
        {
            PreDataSet?.Invoke(player, data);
            _data[player.Lookup] = data;
            OnDataSet?.Invoke(player, data);
        }

        void ICustomDataManager.OnPlayerModRemoved(SNet_Player player)
        {
            if (player.IsBot)
                RemoveData(player);
        }

        private protected override void OnPlayerLeft(SNet_Player player)
        {
            base.OnPlayerLeft(player);
            RemoveData(player);
        }

        /// <summary> Removes the data associated with the given player. Executes OnDataRemoved callbacks.</summary>
        public void RemoveData(SNet_Player player)
        {
            if (_data.Remove(player.Lookup, out var data))
                OnDataRemoved?.Invoke(player, data);
        }

        private protected override void OnLobbyLeft()
        {
            base.OnLobbyLeft();
            _data.Clear();
        }
    }
}
