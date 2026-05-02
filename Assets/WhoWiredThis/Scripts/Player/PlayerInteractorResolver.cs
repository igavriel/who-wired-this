using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Player
{
    /// <summary>
    /// Resolves <see cref="AllowedPlayerTag"/> from an interaction root by walking parents for PlayerA / PlayerB tags.
    /// Shared by <see cref="WhoWiredThis.Visibility.MultiDimensionSubjectCycler"/> and puzzle submit flows.
    /// </summary>
    public static class PlayerInteractorResolver
    {
        private const string PlayerATag = "PlayerA";
        private const string PlayerBTag = "PlayerB";

        public static bool TryResolve(Transform start, out AllowedPlayerTag playerTag)
        {
            playerTag = AllowedPlayerTag.Any_Player;
            if (start == null)
            {
                return false;
            }

            Transform current = start;
            while (current != null)
            {
                if (current.CompareTag(PlayerATag))
                {
                    playerTag = AllowedPlayerTag.Player_A;
                    return true;
                }

                if (current.CompareTag(PlayerBTag))
                {
                    playerTag = AllowedPlayerTag.Player_B;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
