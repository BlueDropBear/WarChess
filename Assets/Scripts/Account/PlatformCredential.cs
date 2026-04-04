using System;

namespace WarChess.Account
{
    /// <summary>
    /// A credential linking a platform identity to a canonical account.
    /// Uses string for Platform because Unity's JsonUtility cannot reliably
    /// serialize enums inside generic lists.
    /// </summary>
    [Serializable]
    public class PlatformCredential
    {
        /// <summary>Platform name (AuthPlatform enum value as string).</summary>
        public string Platform;

        /// <summary>Platform-specific user ID (e.g., Steam64 ID, Apple sub, Google Play ID).</summary>
        public string PlatformUserId;

        /// <summary>Display name from the platform.</summary>
        public string PlatformDisplayName;

        /// <summary>Auth token from the platform. Not persisted locally.</summary>
        [NonSerialized]
        public string AuthToken;

        public PlatformCredential()
        {
            Platform = AuthPlatform.None.ToString();
            PlatformUserId = "";
            PlatformDisplayName = "";
        }

        public PlatformCredential(AuthPlatform platform, string platformUserId, string displayName)
        {
            Platform = platform.ToString();
            PlatformUserId = platformUserId;
            PlatformDisplayName = displayName;
        }

        /// <summary>Returns the AuthPlatform enum value parsed from the Platform string.</summary>
        public AuthPlatform GetPlatform()
        {
            if (Enum.TryParse<AuthPlatform>(Platform, out var result))
                return result;
            return AuthPlatform.None;
        }
    }
}
