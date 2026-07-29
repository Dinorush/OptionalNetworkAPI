using GTFO.API;
using OptionalNetworking.Managers;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OptionalNetworking
{
    internal interface ICustomDataSender
    {
        public void SendDefaultData(SNet_Player player);
        void OnPlayerLeft(SNet_Player player);
        void OnLobbyLeft();
    }

    /// <summary> Determines who data is sent to.</summary>
    [Flags]
    public enum SenderMode
    {
        /// <summary> Send to other players with the mod.</summary>
        OtherPlayers = 1,
        /// <summary> Sends bot info to all players with the mod.</summary>
        Bots = 1 << 2,
        /// <summary> Sends the local player's info to themselves.</summary>
        Self = 1 << 3,
        /// <summary> Sends and receives local and bot data to all players with the mod.</summary>
        All = OtherPlayers | Bots | Self
    }

    /// <summary>
    /// Helper that sends custom data to players with the mod.
    /// </summary>
    /// <typeparam name="T">The struct that will be sent over the network.</typeparam>
    public class CustomDataSender<T> : ICustomDataSender where T : unmanaged
    {
        struct CustomPacket
        {
            public SNetStructs.pPlayer Player;
            public T Data;

            public CustomPacket(SNet_Player player, T data)
            {
                Player = default;
                Player.SetPlayer(player);
                Data = data;
            }
        }

        private const string EventPrefix = $"OptionalData";
        private static readonly byte[] s_packetBuffer = new byte[Marshal.SizeOf<SNetStructs.pPlayer>() + Marshal.SizeOf<T>()];
        private readonly ModInfo _parent;
        private readonly string _eventName;
        private readonly Func<SNet_Player, T>? _defaultProvider;
        private protected Action<SNet_Player, T> ReceiveEvent;
        private readonly Dictionary<ulong, SNet_Player> _sentBots = new();
        private readonly SenderMode _mode;

        private CustomDataSender(ModInfo parent, string id, Action<SNet_Player, T> receiveEvent, Func<SNet_Player, T>? defaultProvider, SenderMode senderMode) : this(parent, id, defaultProvider, senderMode)
        {
            ReceiveEvent = receiveEvent;
        }

        private protected CustomDataSender(ModInfo parent, string id, Func<SNet_Player, T>? defaultProvider, SenderMode senderMode)
        {
            _parent = parent;
            _eventName = EventPrefix + id;
            _defaultProvider = defaultProvider;
            ReceiveEvent = null!; // Must be set by subclass!
            _mode = senderMode;
            NetworkAPI.RegisterFreeSizedEvent(_eventName, ReceiveDataRaw);
        }

        internal static CustomDataSender<T> CreateDataSender(ModInfo parent, int index, Action<SNet_Player, T> receiveEvent, Func<SNet_Player, T>? defaultProvider, SenderMode senderMode)
        {
            return new CustomDataSender<T>(parent, $"{parent.Hash:x}_{index}", receiveEvent, defaultProvider, senderMode);
        }

        /// <summary>
        /// Sends default data to the target player.
        /// </summary>
        public void SendDefaultData(SNet_Player player)
        {
            if (_defaultProvider == null) return;

            if (!player.IsLocal && !player.IsBot)
            {
                if (!_mode.HasFlag(SenderMode.OtherPlayers)) return;

                SendData(SNet.LocalPlayer, _defaultProvider(SNet.LocalPlayer), player);
                if (SNet.IsMaster)
                {
                    foreach (var bot in _sentBots.Values)
                        SendData(bot, _defaultProvider(bot), player);
                }
                return;
            }

            if (_mode.HasFlag(SenderMode.Self) && player.IsLocal)
                ReceiveEvent(player, _defaultProvider(player));
            else if (_mode.HasFlag(SenderMode.Bots) && player.IsBot && SNet.IsMaster && _sentBots.TryAdd(player.Lookup, player))
                SendData(player, _defaultProvider(player));
        }

        /// <summary>
        /// Sends the given data as the local player's data to the target player.
        /// If null, sends it to all players.
        /// </summary>
        public void SendData(T data, SNet_Player? target = null, SNet_ChannelType channelType = SNet_ChannelType.SessionOrderCritical) => SendData(SNet.LocalPlayer, data, target, channelType);
        /// <summary>
        /// Sends the given data as the given player's data to the target player.
        /// If null, sends it to all players.
        /// </summary>
        public void SendData(SNet_Player player, T data, SNet_Player? target = null, SNet_ChannelType channelType = SNet_ChannelType.SessionOrderCritical)
        {
            if (!_mode.HasFlag(SenderMode.Bots) && player.IsBot) return;

            CustomPacket packet = new(player, data);
            if (target == null)
            {
                if (_mode.HasFlag(SenderMode.Self) && player.IsLocal)
                    ReceiveEvent(player, packet.Data);
                if (player.IsBot)
                    ReceiveEvent(player, packet.Data);
                if (_mode.HasFlag(SenderMode.OtherPlayers))
                {
                    if (!player.IsLocal && !player.IsBot)
                        ReceiveEvent(player, packet.Data);
                    foreach (var remotePlayer in _parent.OtherPlayers)
                        NetworkAPI.InvokeFreeSizedEvent(_eventName, ConvertPacket(packet), remotePlayer, channelType);
                }
                return;
            }

            if (target.IsLocal)
            {
                if (_mode.HasFlag(SenderMode.Self))
                    ReceiveEvent(player, packet.Data);
                return;
            }

            if (target.IsBot)
            {
                if (SNet.IsMaster)
                {
                    ReceiveEvent(player, packet.Data);
                    return;
                }
                target = SNet.Master;
            }

            if (ModManager.HasMod(target, _parent))
                NetworkAPI.InvokeFreeSizedEvent(_eventName, ConvertPacket(packet), target, channelType);
        }

        private static byte[] ConvertPacket(CustomPacket packet)
        {
            MemoryMarshal.Write(s_packetBuffer, ref packet);
            return s_packetBuffer;
        }

        private void ReceiveDataRaw(ulong _, byte[] bytes) => ReceiveData(MemoryMarshal.Read<CustomPacket>(bytes));

        private void ReceiveData(CustomPacket packet)
        {
            if (packet.Player.TryGetPlayer(out var player))
                ReceiveEvent(player, packet.Data);
        }

        void ICustomDataSender.OnPlayerLeft(SNet_Player player) => OnPlayerLeft(player);
        private protected virtual void OnPlayerLeft(SNet_Player player)
        {
            _sentBots.Remove(player.Lookup);
        }
        void ICustomDataSender.OnLobbyLeft() => OnLobbyLeft();
        private protected virtual void OnLobbyLeft()
        {
            _sentBots.Clear();
        }
    }
}
