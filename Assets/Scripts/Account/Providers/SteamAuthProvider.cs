#if UNITY_STANDALONE || UNITY_EDITOR
using System;

namespace WarChess.Account
{
    /// <summary>
    /// Steam authentication provider. Authenticates via Steamworks.NET.
    /// Currently a skeleton — replace TODO sections when Steamworks.NET is integrated.
    /// </summary>
    public class SteamAuthProvider : IAuthProvider
    {
        /// <summary>The platform this provider authenticates for.</summary>
        public AuthPlatform Platform => AuthPlatform.Steam;

        /// <summary>Returns true if the user is currently signed in via Steam.</summary>
        public bool IsSignedIn => _isSignedIn;

        private bool _isSignedIn;

        /// <summary>
        /// Authenticates with Steam.
        /// Callback: (success, platformUserId, displayName, authToken).
        /// </summary>
        public void Authenticate(Action<bool, string, string, string> onComplete)
        {
            // TODO: Replace with real Steamworks.NET implementation:
            // 1. Check SteamManager.Initialized
            // 2. Get Steam ID via SteamUser.GetSteamID().m_SteamID.ToString()
            // 3. Get display name via SteamFriends.GetPersonaName()
            // 4. Get auth session ticket via SteamUser.GetAuthSessionTicket()
            // 5. Convert ticket bytes to hex string for server validation

            // Stub fallback until Steamworks.NET is integrated
            _isSignedIn = true;
            string deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            string stubId = "steam_" + deviceId.Substring(0, Math.Min(12, deviceId.Length));
            onComplete?.Invoke(true, stubId, "SteamPlayer", "steam_stub_token");
        }

        /// <summary>Signs out from Steam.</summary>
        public void SignOut()
        {
            _isSignedIn = false;
            // TODO: Cancel any active auth session ticket via SteamUser.CancelAuthTicket()
        }
    }
}
#endif
