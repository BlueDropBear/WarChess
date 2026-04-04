#if UNITY_ANDROID
using System;

namespace WarChess.Account
{
    /// <summary>
    /// Google Play Games authentication provider.
    /// Currently a skeleton — replace TODO sections when the Google Play Games
    /// Unity plugin is integrated.
    /// </summary>
    public class GoogleAuthProvider : IAuthProvider
    {
        /// <summary>The platform this provider authenticates for.</summary>
        public AuthPlatform Platform => AuthPlatform.Google;

        /// <summary>Returns true if the user is currently signed in via Google Play Games.</summary>
        public bool IsSignedIn => _isSignedIn;

        private bool _isSignedIn;

        /// <summary>
        /// Authenticates with Google Play Games Services.
        /// Callback: (success, platformUserId, displayName, authToken).
        /// </summary>
        public void Authenticate(Action<bool, string, string, string> onComplete)
        {
            // TODO: Replace with real Google Play Games implementation:
            // 1. Call PlayGamesPlatform.Instance.Authenticate()
            // 2. On success, get player ID via PlayGamesPlatform.Instance.GetUserId()
            // 3. Get display name via PlayGamesPlatform.Instance.GetUserDisplayName()
            // 4. Request server auth code via PlayGamesPlatform.Instance.RequestServerSideAccess()
            //    for server-side token validation

            // Stub fallback until Google Play Games plugin is integrated
            _isSignedIn = true;
            string deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            string stubId = "google_" + deviceId.Substring(0, Math.Min(12, deviceId.Length));
            onComplete?.Invoke(true, stubId, "GooglePlayer", "google_stub_token");
        }

        /// <summary>Signs out from Google Play Games.</summary>
        public void SignOut()
        {
            _isSignedIn = false;
            // TODO: Call PlayGamesPlatform.Instance.SignOut()
        }
    }
}
#endif
