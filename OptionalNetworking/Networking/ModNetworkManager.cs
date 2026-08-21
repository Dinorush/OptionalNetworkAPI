using GTFO.API;
using OptionalNetworking.Managers;
using SNetwork;
using System;
using System.Buffers.Binary;

namespace OptionalNetworking.Networking
{
    internal static class ModNetworkManager
    {
        private const string ModSetEvent = $"{EntryPoint.MODNAME}ModSet";
        private const string ModHandshakeEvent = $"{EntryPoint.MODNAME}ModHandshake";

        public static void Init()
        {
            NetworkAPI.RegisterFreeSizedEvent(ModSetEvent, ReceiveModSet);
            NetworkAPI.RegisterEvent<byte>(ModHandshakeEvent, ReceiveModHandshake);
        }

        internal static void SendModSet(SNet_Player player)
        {
            byte[] dataArr = new byte[ModManager.ModHashes.Count * 8];
            int count = 0;
            foreach (var hash in ModManager.ModHashes)
                BinaryPrimitives.WriteUInt64BigEndian(dataArr.AsSpan(count++ * 8, 8), hash);
            NetworkAPI.InvokeFreeSizedEvent(ModSetEvent, dataArr, player, SNet_ChannelType.SessionOrderCritical);
        }

        private static void ReceiveModSet(ulong lookup, byte[] data)
        {
            if (!SNet.TryGetPlayer(lookup, out var player)) return;

            ulong[] hashes = new ulong[data.Length / 8];
            int count = 0;
            for (int i = 0; i < data.Length; i += 8)
                hashes[count++] = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(i, 8));
            
            ModManager.OnPlayerModSet(player, ModManager.HashesToMask(hashes));
            ModHandshakeHandler.OnReceiveModSet(player);
        }

        internal static void SendModHandshake(SNet_Player player, eHandshakeStatus status)
        {
            NetworkAPI.InvokeEvent(ModHandshakeEvent, (byte)status, player, SNet_ChannelType.SessionOrderCritical);
        }

        private static void ReceiveModHandshake(ulong lookup, byte status)
        {
            if (!SNet.TryGetPlayer(lookup, out var player)) return;

            ModHandshakeHandler.OnReceiveHandshake(player, (eHandshakeStatus) status);
        }
    }
}
