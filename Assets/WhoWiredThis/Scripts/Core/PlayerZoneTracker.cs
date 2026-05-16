using System;
using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Tracks room/zone name independently for Player A and Player B (dual-display HUD).
    /// </summary>
    public class PlayerZoneTracker : MonoBehaviour
    {
        public static PlayerZoneTracker Instance { get; private set; }

        [SerializeField] private string defaultZoneName = "Relay Room";

        public string CurrentZonePlayerA { get; private set; }
        public string CurrentZonePlayerB { get; private set; }

        public event Action<string> OnPlayerAZoneChanged;
        public event Action<string> OnPlayerBZoneChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentZonePlayerA = defaultZoneName;
            CurrentZonePlayerB = defaultZoneName;
        }

        public string GetZone(AllowedPlayerTag player)
        {
            return player switch
            {
                AllowedPlayerTag.Player_A => CurrentZonePlayerA,
                AllowedPlayerTag.Player_B => CurrentZonePlayerB,
                _ => defaultZoneName
            };
        }

        public void SetZone(AllowedPlayerTag player, string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName))
            {
                return;
            }

            switch (player)
            {
                case AllowedPlayerTag.Player_A:
                    if (CurrentZonePlayerA == zoneName)
                    {
                        return;
                    }

                    CurrentZonePlayerA = zoneName;
                    OnPlayerAZoneChanged?.Invoke(zoneName);
                    break;

                case AllowedPlayerTag.Player_B:
                    if (CurrentZonePlayerB == zoneName)
                    {
                        return;
                    }

                    CurrentZonePlayerB = zoneName;
                    OnPlayerBZoneChanged?.Invoke(zoneName);
                    break;
            }
        }
    }
}
