using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using SNetwork;
using System.Collections.Generic;
using UnityEngine;

namespace OptionalNetworking.Networking
{
    internal class ModHandshakeHandler : MonoBehaviour
    {
        public static ModHandshakeHandler Instance { get; private set; } = null!;

        internal static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ModHandshakeHandler>();
            AssetAPI.OnStartupAssetsLoaded += () =>
            {
                GameObject go = new(EntryPoint.MODNAME + "Handshake");
                GameObject.DontDestroyOnLoad(go);
                go.AddComponent<ModHandshakeHandler>();
            };
        }

        internal static void OnAddPlayer(SNet_Player player) => Instance.AddPlayer(player);
        internal static void OnReceiveModSet(SNet_Player player) => Instance.ReceiveModSet(player);
        internal static void OnReceiveHandshake(SNet_Player player, eHandshakeStatus status) => Instance.ReceiveHandshake(player, status);
        internal static void OnRemovePlayer(SNet_Player player) => Instance._handshakes.Remove(player.Lookup);
        internal static void OnLobbyLeft() => Instance._handshakes.Clear();

        class HandshakeInstance
        {
            private readonly SNet_Player _player;
            private eHandshakeStatus _status;
            private int _initiateAttempts = 0;
            private const int MaxInitiateAttempts = 4;
            private float _nextHandshakeTime = 0;
            private const float HandshakeInterval = 5f;

            public eHandshakeStatus Status
            {
                get => _status;
                set
                {
                    if (_status == value) return;

                    _status = value;
                    _nextHandshakeTime = Clock.Time + HandshakeInterval;
                }
            }

            public HandshakeInstance(SNet_Player player, eHandshakeStatus status = eHandshakeStatus.Initiated)
            {
                _player = player;
                _status = status;
                _nextHandshakeTime = Clock.Time + HandshakeInterval;
            }

            public bool UpdateCheckDone()
            {
                switch (Status)
                {
                    case eHandshakeStatus.CompleteButMissed:
                    case eHandshakeStatus.Completed:
                    case eHandshakeStatus.Failed:
                        return true;
                }

                var time = Clock.Time;
                if (time < _nextHandshakeTime) return false;

                if (_status == eHandshakeStatus.Initiated && _initiateAttempts++ == MaxInitiateAttempts)
                {
                    Status = eHandshakeStatus.Failed;
                    return true;
                }

                SendMessage();
                _nextHandshakeTime = time + HandshakeInterval;
                return false;
            }

            public void SendMessage()
            {
                switch (Status)
                {
                    case eHandshakeStatus.Initiated:
                        ModNetworkManager.SendModSet(_player);
                        break;
                    case eHandshakeStatus.Received:
                    case eHandshakeStatus.Missed:
                    case eHandshakeStatus.Completed:
                        ModNetworkManager.SendModHandshake(_player, Status);
                        break;
                }
            }
        }

        private readonly Dictionary<ulong, HandshakeInstance> _handshakes = new();

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            bool isAllDone = true;
            foreach (var handshake in _handshakes.Values)
                isAllDone &= handshake.UpdateCheckDone();

            if (isAllDone)
                enabled = false;
        }

        private void ReceiveModSet(SNet_Player player)
        {
            if (!_handshakes.TryGetValue(player.Lookup, out var handshake))
            {
                _handshakes.Add(player.Lookup, handshake = new(player, eHandshakeStatus.Received));
                ModNetworkManager.SendModSet(player);
            }

            switch (handshake.Status)
            {
                case eHandshakeStatus.Initiated:
                case eHandshakeStatus.Failed:
                    handshake.Status = eHandshakeStatus.Received;
                    break;
                case eHandshakeStatus.Missed:
                    handshake.Status = eHandshakeStatus.Completed;
                    break;
            }
            handshake.SendMessage();
            enabled = true;
        }

        private void ReceiveHandshake(SNet_Player player, eHandshakeStatus status)
        {
            if (!_handshakes.TryGetValue(player.Lookup, out var handshake))
                _handshakes.Add(player.Lookup, handshake = new(player));

            switch (status)
            {
                case eHandshakeStatus.Missed:
                    handshake.Status = eHandshakeStatus.CompleteButMissed;
                    ModNetworkManager.SendModSet(player);
                    return;
                case eHandshakeStatus.Received:
                    switch (handshake.Status)
                    {
                        case eHandshakeStatus.Initiated:
                        case eHandshakeStatus.Failed:
                            handshake.Status = eHandshakeStatus.Missed;
                            break;
                        case eHandshakeStatus.Received:
                            handshake.Status = eHandshakeStatus.Completed;
                            break;
                        case eHandshakeStatus.Completed:
                            break;
                        default:
                            return;
                    }
                    handshake.SendMessage();
                    break;
                case eHandshakeStatus.Completed:
                    handshake.Status = eHandshakeStatus.Completed;
                    return;
            }

            enabled = true;
        }

        private void AddPlayer(SNet_Player player)
        {
            if (player.IsBot || player.IsLocal || _handshakes.ContainsKey(player.Lookup)) return;

            _handshakes.Add(player.Lookup, new(player));
            ModNetworkManager.SendModSet(player);
            enabled = true;
        }
    }
}
