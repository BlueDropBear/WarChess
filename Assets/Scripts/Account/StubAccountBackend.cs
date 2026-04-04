using System;
using System.Collections.Generic;

namespace WarChess.Account
{
    /// <summary>
    /// In-memory stub implementation of IAccountBackend for development.
    /// Simulates server-side account resolution and linking using dictionaries.
    /// Replace with a real backend (PlayFab, Firebase, Nakama, custom) for production.
    /// </summary>
    public class StubAccountBackend : IAccountBackend
    {
        // Maps "platform:userId" -> canonical user ID
        private readonly Dictionary<string, string> _platformToCanonical;

        // Maps canonical user ID -> list of linked platform credentials
        private readonly Dictionary<string, List<PlatformCredential>> _canonicalToCredentials;

        /// <summary>Creates a new stub backend with empty state.</summary>
        public StubAccountBackend()
        {
            _platformToCanonical = new Dictionary<string, string>();
            _canonicalToCredentials = new Dictionary<string, List<PlatformCredential>>();
        }

        /// <summary>
        /// Resolves an existing canonical user ID for the given platform credential,
        /// or creates a new account if this platform user has never been seen.
        /// </summary>
        public void ResolveOrCreateAccount(AuthPlatform platform, string platformUserId,
            string authToken, Action<bool, string> onComplete)
        {
            string key = MakePlatformKey(platform, platformUserId);

            if (_platformToCanonical.TryGetValue(key, out string existingCanonicalId))
            {
                // Existing account found
                onComplete?.Invoke(true, existingCanonicalId);
                return;
            }

            // Create new canonical account
            string newCanonicalId = Guid.NewGuid().ToString("N");
            _platformToCanonical[key] = newCanonicalId;

            var credential = new PlatformCredential(platform, platformUserId, "");
            _canonicalToCredentials[newCanonicalId] = new List<PlatformCredential> { credential };

            onComplete?.Invoke(true, newCanonicalId);
        }

        /// <summary>
        /// Links a new platform credential to an existing canonical user.
        /// Returns ConflictRequiresMerge if the platform user ID already
        /// belongs to a different canonical account.
        /// </summary>
        public void LinkPlatform(string canonicalUserId, AuthPlatform platform,
            string platformUserId, string authToken, Action<LinkResult> onComplete)
        {
            string key = MakePlatformKey(platform, platformUserId);

            // Check if this platform user is already linked somewhere
            if (_platformToCanonical.TryGetValue(key, out string existingCanonicalId))
            {
                if (existingCanonicalId == canonicalUserId)
                {
                    onComplete?.Invoke(LinkResult.AlreadyLinked);
                    return;
                }

                // Conflict: this platform user belongs to a different account
                onComplete?.Invoke(LinkResult.ConflictRequiresMerge);
                return;
            }

            // Link the platform to the canonical account
            _platformToCanonical[key] = canonicalUserId;

            if (!_canonicalToCredentials.ContainsKey(canonicalUserId))
                _canonicalToCredentials[canonicalUserId] = new List<PlatformCredential>();

            _canonicalToCredentials[canonicalUserId].Add(
                new PlatformCredential(platform, platformUserId, ""));

            onComplete?.Invoke(LinkResult.Success);
        }

        /// <summary>
        /// Gets all platforms linked to a canonical user ID.
        /// </summary>
        public void GetLinkedPlatforms(string canonicalUserId,
            Action<bool, List<PlatformCredential>> onComplete)
        {
            if (_canonicalToCredentials.TryGetValue(canonicalUserId, out var credentials))
            {
                onComplete?.Invoke(true, new List<PlatformCredential>(credentials));
                return;
            }

            onComplete?.Invoke(true, new List<PlatformCredential>());
        }

        private static string MakePlatformKey(AuthPlatform platform, string platformUserId)
        {
            return platform.ToString() + ":" + platformUserId;
        }
    }
}
