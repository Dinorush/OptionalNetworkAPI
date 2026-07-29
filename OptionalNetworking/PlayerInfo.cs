using Player;
using SNetwork;

namespace OptionalNetworking
{
    /// <summary> Wrapper class for storing a player's mod and characteristics.</summary>
    public class PlayerInfo
    {
        /// <summary> Masks that store which mods the player has installed.</summary>
        public (long mask1, long mask2) ModMask { get; internal set; } = (0, 1);
        /// <summary> Cached SNet_Player lookup.</summary>
        public ulong Lookup { get; }
        /// <summary> The SNet_Player this class wraps.</summary>
        public SNet_Player Owner { get; }
        /// <summary> The current PlayerAgent instance.</summary>
        public PlayerAgent? Player { get; internal set; }
        /// <summary> If the player is locally owned.</summary>
        public bool IsLocal { get; }
        /// <summary> If the player is a bot.</summary>
        public bool IsBot { get; }
        /// <summary> If the player is not locally owned, excluding bots.</summary>
        public bool IsRemote { get; }

        internal PlayerInfo(SNet_Player owner)
        {
            Owner = owner;
            Lookup = Owner.Lookup;
            IsLocal = Owner.IsLocal;
            IsBot = Owner.IsBot;
            IsRemote = !Owner.IsBot && !Owner.IsLocal;
        }

        /// <summary> Returns whether this player has the given mod.</summary>
        public bool HasMod(ModInfo modInfo) => HasMod(modInfo.Mask);
        /// <summary> Returns whether this player has the given mod mask.</summary>
        /// <param name="modMask"> A mod mask. Should be obtained from a ModInfo object.</param>
        public bool HasMod(long modMask)
        {
            if ((modMask & 1) == 0)
                return (ModMask.mask1 & modMask) != 0;
            else
                return (ModMask.mask2 & modMask) != 1;
        }
    }
}
