using System;

namespace WarChess.Account
{
    /// <summary>
    /// Orchestrates cross-platform account linking.
    /// Handles the flow of associating a new platform credential with an
    /// existing canonical account, including conflict detection when the
    /// platform user already belongs to a different account.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class AccountLinker
    {
        private readonly IAccountBackend _backend;

        /// <summary>Fired when a link attempt completes with its result.</summary>
        public event Action<LinkResult> OnLinkCompleted;

        /// <summary>
        /// Fired when a link attempt finds the platform user already belongs
        /// to a different canonical account. Parameters: (currentCanonicalId, conflictingCanonicalId).
        /// The UI should prompt the user to choose which account to keep.
        /// </summary>
        public event Action<string, string> OnMergeRequired;

        /// <summary>Creates an account linker backed by the given backend service.</summary>
        public AccountLinker(IAccountBackend backend)
        {
            _backend = backend;
        }

        /// <summary>
        /// Attempts to link a new platform credential to the current canonical account.
        /// If the platform user already belongs to a different account, fires OnMergeRequired.
        /// </summary>
        /// <param name="currentCanonicalId">The user's current canonical account ID.</param>
        /// <param name="platform">The platform being linked.</param>
        /// <param name="platformUserId">The user's ID on the platform being linked.</param>
        /// <param name="authToken">Auth token from the platform for server validation.</param>
        public void LinkNewPlatform(string currentCanonicalId, AuthPlatform platform,
            string platformUserId, string authToken)
        {
            _backend.LinkPlatform(currentCanonicalId, platform, platformUserId, authToken,
                result =>
                {
                    if (result == LinkResult.ConflictRequiresMerge)
                    {
                        // Resolve the conflicting canonical ID so UI can present the choice
                        _backend.ResolveOrCreateAccount(platform, platformUserId, authToken,
                            (ok, conflictingId) =>
                            {
                                if (ok)
                                    OnMergeRequired?.Invoke(currentCanonicalId, conflictingId);
                            });
                    }

                    OnLinkCompleted?.Invoke(result);
                });
        }
    }
}
