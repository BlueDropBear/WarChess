using System;

namespace WarChess.Account
{
    /// <summary>
    /// Development stub auth provider. Generates a deterministic fake identity
    /// based on the device unique identifier. Used in Editor and non-platform builds.
    /// </summary>
    public class StubAuthProvider : IAuthProvider
    {
        /// <summary>The platform this provider authenticates for.</summary>
        public AuthPlatform Platform => AuthPlatform.DevStub;

        /// <summary>Returns true if currently signed in.</summary>
        public bool IsSignedIn => _isSignedIn;

        private bool _isSignedIn;
        private readonly string _stubUserId;
        private readonly string _stubDisplayName;

        /// <summary>
        /// Creates a stub auth provider with an optional fixed user ID.
        /// If not provided, generates one from the device unique identifier.
        /// </summary>
        public StubAuthProvider(string stubUserId = null, string displayName = null)
        {
            _stubUserId = stubUserId ?? GenerateDeviceId();
            _stubDisplayName = displayName ?? "DevPlayer";
        }

        /// <summary>
        /// Authenticates immediately with a fake identity.
        /// Callback: (success, platformUserId, displayName, authToken).
        /// </summary>
        public void Authenticate(Action<bool, string, string, string> onComplete)
        {
            _isSignedIn = true;
            onComplete?.Invoke(true, _stubUserId, _stubDisplayName, "stub_token_" + _stubUserId);
        }

        /// <summary>Signs out from the stub provider.</summary>
        public void SignOut()
        {
            _isSignedIn = false;
        }

        private static string GenerateDeviceId()
        {
            // Use a hash of device identifier for deterministic stub IDs
            string deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            return "dev_" + deviceId.Substring(0, Math.Min(12, deviceId.Length));
        }
    }
}
