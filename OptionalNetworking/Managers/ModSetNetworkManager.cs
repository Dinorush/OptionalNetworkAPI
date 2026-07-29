using GTFO.API;
using SNetwork;
using System;
using System.Buffers.Binary;

namespace OptionalNetworking.Managers
{
    internal static class ModSetNetworkManager
    {
        private const string ModSetEvent = $"{EntryPoint.MODNAME}ModSet";

        public static void Init()
        {
            NetworkAPI.RegisterFreeSizedEvent(ModSetEvent, ReceiveModSet);
        }

        internal static void OnAddPlayer(SNet_Player player)
        {
            if (player.IsBot || player.IsLocal) return;

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
        }
    }
}
