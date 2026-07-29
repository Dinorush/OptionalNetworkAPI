using OptionalNetworking.Managers;
using SNetwork;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OptionalNetworking
{
    public static class OptionalNetworkAPI
    {
        /// <summary> Registers a mod with the given name. The name must be unique.</summary>
        public static ModInfo RegisterMod(string name) => ModManager.AddMod(name);

        /// <summary> Returns whether the given mod name has been registered.</summary>
        public static bool HasMod(string name) => ModManager.HasMod(name);
        /// <summary> Returns whether the specified player has the given mod name.</summary>
        public static bool HasMod(SNet_Player player, string name) => ModManager.HasMod(player, name);
        /// <summary> Returns whether the specified player has the given mod.</summary>
        public static bool HasMod(SNet_Player player, ModInfo modInfo) => ModManager.HasMod(player, modInfo);
        /// <summary> Returns the ModInfo wrapper for the given mod name.</summary>
        public static bool TryGetModInfo(string name, [MaybeNullWhen(false)] out ModInfo modInfo) => ModManager.TryGetModInfo(name, out modInfo);
        /// <summary> Returns the PlayerInfo wrapper for the given player.</summary>
        public static bool TryGetPlayerInfo(SNet_Player player, [MaybeNullWhen(false)] out PlayerInfo playerInfo) => ModManager.TryGetPlayerInfo(player, out playerInfo);

        /// <summary> The PlayerInfo wrapper for the master.</summary>
        public static PlayerInfo? MasterInfo => ModManager.MasterInfo;
        /// <summary> The PlayerInfo wrapper for the local player.</summary>
        public static PlayerInfo? LocalInfo => ModManager.LocalInfo;

        /// <summary> The collection of registered mods.</summary>
        public static IReadOnlyCollection<ModInfo> Mods => ModManager.Mods;
    }
}
