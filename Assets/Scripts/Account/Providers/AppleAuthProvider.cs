#if UNITY_IOS
using System;

namespace WarChess.Account
{
    /// <summary>
    /// Apple authentication provider. Uses Sign in with Apple.
    /// Currently a skeleton — replace TODO sections when the Apple Sign In
    /// Unity plugin is integrated.
    /// </summary>
    public class AppleAuthProvider : IAuthProvider
    {
        /// <summary>The platform this provider authenticates for.</summary>
        public AuthPlatform Platform => AuthPlatform.Apple;

        /// <summary>Returns true if the user is currently signed in via Apple.</summary>
        public bool IsSignedIn => _isSignedIn;

        private bool _isSignedIn;

        /// <summary>
        /// Authenticates with Sign in with Apple.
        /// Callback: (success, platformUserId, displayName, authToken).
        /// </summary>
        public void Authenticate(Action<bool, string, string, string> onComplete)
        {
            // TODO: Replace with real Sign in with Apple implementation:
            // 1. Create an AppleAuthManager instance
            // 2. Call LoginWithAppleId() with requested scopes (FullName, Email)
            // 3. On success, extract:
            //    - credential.User (Apple user ID, stable across sessions)
            //    - credential.FullName (only returned on first sign-in)
            //    - credential.IdentityToken (JWT for server validation)
            // 4. Cache the user ID for quick-check credential state

            // Stub fallback until Apple Sign In plugin is integrated
            _isSignedIn = true;
            string deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            string stubId = "apple_" + deviceId.Substring(0, Math.Min(12, deviceId.Length));
            onComplete?.Invoke(true, stubId, "ApplePlayer", "apple_stub_token");
        }

        /// <summary>Signs out from Apple.</summary>
        public void SignOut()
        {
            _isSignedIn = false;
            // Note: Sign in with Apple does not have a client-side sign-out API.
            // Signing out is handled by clearing local state.
        }
    }
}
#endif
