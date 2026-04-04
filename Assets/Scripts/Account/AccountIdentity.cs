using System;
using System.Collections.Generic;

namespace WarChess.Account
{
    /// <summary>
    /// Cached account identity stored locally for offline access.
    /// Maps the player to a canonical backend user ID and tracks
    /// all linked platform credentials.
    /// </summary>
    [Serializable]
    public class AccountIdentity
    {
        /// <summary>Backend-generated UUID — the single source of identity.</summary>
        public string CanonicalUserId;

        /// <summary>Which platform originally created this account (AuthPlatform as string).</summary>
        public string PrimaryPlatform;

        /// <summary>All platform credentials linked to this canonical account.</summary>
        public List<PlatformCredential> LinkedPlatforms;

        /// <summary>UTC ticks of last successful authentication.</summary>
        public long LastAuthenticatedTicks;

        public AccountIdentity()
        {
            CanonicalUserId = "";
            PrimaryPlatform = AuthPlatform.None.ToString();
            LinkedPlatforms = new List<PlatformCredential>();
            LastAuthenticatedTicks = 0;
        }

        /// <summary>Returns true if a canonical user ID has been resolved.</summary>
        public bool HasAccount => !string.IsNullOrEmpty(CanonicalUserId);

        /// <summary>Returns the primary AuthPlatform enum value.</summary>
        public AuthPlatform GetPrimaryPlatform()
        {
            if (Enum.TryParse<AuthPlatform>(PrimaryPlatform, out var result))
                return result;
            return AuthPlatform.None;
        }

        /// <summary>Checks if a specific platform is already linked.</summary>
        public bool IsPlatformLinked(AuthPlatform platform)
        {
            string platformStr = platform.ToString();
            for (int i = 0; i < LinkedPlatforms.Count; i++)
            {
                if (LinkedPlatforms[i].Platform == platformStr)
                    return true;
            }
            return false;
        }

        /// <summary>Adds or updates a linked platform credential.</summary>
        public void AddOrUpdateLinkedPlatform(PlatformCredential credential)
        {
            for (int i = 0; i < LinkedPlatforms.Count; i++)
            {
                if (LinkedPlatforms[i].Platform == credential.Platform)
                {
                    LinkedPlatforms[i] = credential;
                    return;
                }
            }
            LinkedPlatforms.Add(credential);
        }
    }
}
