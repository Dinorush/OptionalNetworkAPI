using GTFO.API;
using SNetwork;

namespace OptionalNetworking.Managers
{
    internal static class ModSetNetworkManager
    {
        private const string ModSetEvent = $"{EntryPoint.MODNAME}ModSet";

        public static void Init()
        {
            NetworkAPI.RegisterEvent<ModSetData>(ModSetEvent, ReceiveModSet);
        }

        internal static void OnAddPlayer(SNet_Player player)
        {
            if (!player.IsBot)
                NetworkAPI.InvokeEvent(ModSetEvent, new ModSetData(ModManager.LocalModMask), player, SNet_ChannelType.SessionOrderCritical);
        }

        private static void ReceiveModSet(ulong lookup, ModSetData data)
        {
            if (!SNet.TryGetPlayer(lookup, out var player)) return;

            ModManager.OnPlayerModSet(player, (data.Mask1, data.Mask2));
        }

        struct ModSetData
        {
            public long Mask1;
            public long Mask2;

            public ModSetData((long mask1, long mask2) maskPair)
            {
                Mask1 = maskPair.mask1;
                Mask2 = maskPair.mask2;
            }
        }
    }
}
