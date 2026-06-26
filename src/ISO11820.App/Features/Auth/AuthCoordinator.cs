namespace ISO11820.App.Features.Auth;

/// <summary>
/// Shell placeholder — no validation logic.
/// Real authentication (DB-backed role/password check) will be
/// added by the persistence owner once the access API is available.
/// </summary>
public sealed class AuthCoordinator
{
    /// <summary>
    /// Always accepts. Returns success for any input.
    /// Will be replaced with real validation by the persistence owner.
    /// </summary>
    public (bool Success, string? ErrorMessage) TryLogin(string role, string password)
    {
        return (true, null);
    }
}
