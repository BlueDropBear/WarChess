using System;

namespace WarChess.Account
{
    /// <summary>
    /// Manages player authentication and account identity.
    /// Owns the auth state, wires platform providers to the backend,
    /// and provides account linking capabilities.
    /// Pure C# — no MonoBehaviour. Follows the MonetizationManager pattern.
    /// </summary>
    public class AccountManager
    {
        private IAuthProvider _authProvider;
        private IAccountBackend _backend;
        private readonly AccountLinker _linker;
        private AccountIdentity _identity;
        private AuthState _state;

        /// <summary>Fired when the authentication state changes.</summary>
        public event Action<AuthState> OnAuthStateChanged;

        /// <summary>Fired when a canonical account identity is resolved.</summary>
        public event Action<AccountIdentity> OnAccountResolved;

        /// <summary>Fired when a platform link attempt completes.</summary>
        public event Action<LinkResult> OnLinkCompleted;

        /// <summary>
        /// Fired when a link attempt finds a conflict (platform user belongs
        /// to a different account). Parameters: (currentCanonicalId, conflictingCanonicalId).
        /// </summary>
        public event Action<string, string> OnMergeRequired;

        /// <summary>Current authentication state.</summary>
        public AuthState State => _state;

        /// <summary>Cached account identity. Available offline after first auth.</summary>
        public AccountIdentity Identity => _identity;

        /// <summary>Returns true if the user is fully authenticated.</summary>
        public bool IsAuthenticated => _state == AuthState.Authenticated;

        /// <summary>The platform currently used for authentication.</summary>
        public AuthPlatform CurrentPlatform => _authProvider.Platform;

        /// <summary>
        /// Creates the account manager with injected dependencies.
        /// </summary>
        /// <param name="authProvider">Platform-specific auth provider.</param>
        /// <param name="backend">Account backend for canonical ID resolution.</param>
        /// <param name="cachedIdentity">Previously cached identity from SaveData, or null.</param>
        public AccountManager(IAuthProvider authProvider, IAccountBackend backend,
            AccountIdentity cachedIdentity)
        {
            _authProvider = authProvider;
            _backend = backend;
            _identity = cachedIdentity ?? new AccountIdentity();
            _state = AuthState.NotAuthenticated;

            _linker = new AccountLinker(backend);
            _linker.OnLinkCompleted += result => OnLinkCompleted?.Invoke(result);
            _linker.OnMergeRequired += (current, conflict) => OnMergeRequired?.Invoke(current, conflict);
        }

        /// <summary>
        /// Authenticates with the current platform provider and resolves
        /// the canonical account ID via the backend. Auto-called during initialization.
        /// </summary>
        public void Authenticate()
        {
            _state = AuthState.Authenticating;
            OnAuthStateChanged?.Invoke(_state);

            _authProvider.Authenticate((success, platformUserId, displayName, token) =>
            {
                if (!success)
                {
                    _state = AuthState.Failed;
                    OnAuthStateChanged?.Invoke(_state);
                    return;
                }

                // Resolve canonical account from backend
                _backend.ResolveOrCreateAccount(_authProvider.Platform, platformUserId, token,
                    (ok, canonicalId) =>
                    {
                        if (ok)
                        {
                            _identity.CanonicalUserId = canonicalId;
                            _identity.LastAuthenticatedTicks = DateTime.UtcNow.Ticks;

                            // Set primary platform if this is a new account
                            if (_identity.GetPrimaryPlatform() == AuthPlatform.None)
                                _identity.PrimaryPlatform = _authProvider.Platform.ToString();

                            // Update linked platform credential
                            var credential = new PlatformCredential(
                                _authProvider.Platform, platformUserId, displayName);
                            _identity.AddOrUpdateLinkedPlatform(credential);

                            _state = AuthState.Authenticated;
                            OnAccountResolved?.Invoke(_identity);
                        }
                        else
                        {
                            _state = AuthState.Failed;
                        }

                        OnAuthStateChanged?.Invoke(_state);
                    });
            });
        }

        /// <summary>
        /// Initiates linking a new platform to the current account.
        /// Authenticates with the other platform first, then links.
        /// </summary>
        /// <param name="otherProvider">Auth provider for the platform to link.</param>
        public void LinkPlatform(IAuthProvider otherProvider)
        {
            if (!IsAuthenticated || !_identity.HasAccount)
                return;

            otherProvider.Authenticate((success, platformUserId, displayName, token) =>
            {
                if (!success)
                {
                    OnLinkCompleted?.Invoke(LinkResult.Failed);
                    return;
                }

                _linker.LinkNewPlatform(
                    _identity.CanonicalUserId,
                    otherProvider.Platform,
                    platformUserId,
                    token);
            });
        }

        /// <summary>
        /// Signs out from the current platform and resets auth state.
        /// Does not clear the cached identity.
        /// </summary>
        public void SignOut()
        {
            _authProvider.SignOut();
            _state = AuthState.NotAuthenticated;
            OnAuthStateChanged?.Invoke(_state);
        }

        /// <summary>Swaps the active auth provider (e.g., for testing).</summary>
        public void SetAuthProvider(IAuthProvider provider)
        {
            _authProvider = provider;
        }

        /// <summary>Swaps the backend (e.g., from stub to production).</summary>
        public void SetBackend(IAccountBackend backend)
        {
            _backend = backend;
        }
    }
}
