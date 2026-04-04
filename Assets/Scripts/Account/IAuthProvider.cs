using System;

namespace WarChess.Account
{
    /// <summary>
    /// Interface for platform-specific authentication.
    /// Each platform (Steam, Apple, Google) implements this.
    /// Follows the same provider pattern as IPurchaseValidator and IAnalyticsProvider.
    /// </summary>
    public interface IAuthProvider
    {
        /// <summary>The platform this provider authenticates for.</summary>
        AuthPlatform Platform { get; }

        /// <summary>
        /// Authenticates with the platform SDK.
        /// Callback parameters: (success, platformUserId, displayName, authToken).
        /// </summary>
        void Authenticate(Action<bool, string, string, string> onComplete);

        /// <summary>Returns true if the user is currently signed in on this platform.</summary>
        bool IsSignedIn { get; }

        /// <summary>Signs out from the platform.</summary>
        void SignOut();
    }
}
