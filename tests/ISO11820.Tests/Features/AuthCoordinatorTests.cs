using ISO11820.App.Features.Auth;

namespace ISO11820.Tests.Features;

/// <summary>
/// Tests for <see cref="AuthCoordinator"/> shell placeholder.
/// AuthCoordinator is currently a shell that always accepts any login.
/// These tests verify the shell contract — when the persistence owner
/// replaces it with real validation, these tests should be updated accordingly.
/// </summary>
public sealed class AuthCoordinatorTests
{
    private readonly AuthCoordinator _auth = new();

    [Fact]
    public void TryLogin_Always_Succeeds_For_Admin()
    {
        var (success, error) = _auth.TryLogin("管理员", "any");

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public void TryLogin_Always_Succeeds_For_Operator()
    {
        var (success, error) = _auth.TryLogin("操作员", "any");

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public void TryLogin_Always_Succeeds_For_Any_Password()
    {
        var (success, error) = _auth.TryLogin("管理员", "wrong");

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public void TryLogin_Always_Succeeds_For_Empty_Password()
    {
        var (success, error) = _auth.TryLogin("管理员", "");

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public void TryLogin_Always_Succeeds_For_Empty_Role()
    {
        var (success, error) = _auth.TryLogin("", "");

        Assert.True(success);
        Assert.Null(error);
    }
}
