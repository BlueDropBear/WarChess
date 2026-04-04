using System;
using System.Collections.Generic;

namespace WarChess.Account
{
    /// <summary>
    /// Interface for the account backend service.
    /// Handles canonical ID resolution, account linking, and platform mapping.
    /// Separate from IAuthProvider because the platform SDK only answers
    /// "who is this person on my platform?" while the backend answers
    /// "what is their canonical identity in our system?"
    /// </summary>
    public interface IAccountBackend
    {
        /// <summary>
        /// Given a platform credential, resolves an existing canonical user ID
        /// or creates a new account. Callback: (success, canonicalUserId).
        /// </summary>
        void ResolveOrCreateAccount(AuthPlatform platform, string platformUserId,
            string authToken, Action<bool, string> onComplete);

        /// <summary>
        /// Links a new platform credential to an existing canonical user.
        /// Returns ConflictRequiresMerge if the platform user ID already
        /// belongs to a different canonical account.
        /// </summary>
        void LinkPlatform(string canonicalUserId, AuthPlatform platform,
            string platformUserId, string authToken, Action<LinkResult> onComplete);

        /// <summary>
        /// Gets all platforms linked to a canonical user ID.
        /// Callback: (success, list of linked credentials).
        /// </summary>
        void GetLinkedPlatforms(string canonicalUserId,
            Action<bool, List<PlatformCredential>> onComplete);
    }
}
