namespace WarChess.Account
{
    /// <summary>
    /// Supported authentication platforms.
    /// </summary>
    public enum AuthPlatform
    {
        None,
        Steam,
        Apple,
        Google,
        DevStub
    }

    /// <summary>
    /// Current state of the authentication flow.
    /// </summary>
    public enum AuthState
    {
        NotAuthenticated,
        Authenticating,
        Authenticated,
        Failed
    }

    /// <summary>
    /// Result of an account linking attempt.
    /// </summary>
    public enum LinkResult
    {
        Success,
        AlreadyLinked,
        ConflictRequiresMerge,
        Failed,
        InvalidToken
    }
}
