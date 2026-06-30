using ISO11820.App.Features.Auth;
using ISO11820.App.Infrastructure.Persistence;

namespace ISO11820.Tests.Features;

/// <summary>
/// Tests for AuthCoordinator with real database validation
/// </summary>
public sealed class AuthCoordinatorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DbHelper _dbHelper;
    private readonly DatabaseInitializer _initializer;
    private readonly AuthCoordinator _auth;

    public AuthCoordinatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        var dbPath = Path.Combine(_tempDir, "test.db");
        _dbHelper = new DbHelper(dbPath);
        _initializer = new DatabaseInitializer(_dbHelper);
        _initializer.EnsureCreated();
        _auth = new AuthCoordinator(_dbHelper);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                // Close any open DB connections by forcing GC
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures — temp directory will be cleaned up by OS
        }
    }

    [Fact]
    public void TryLogin_Admin_With_Correct_Password_Succeeds()
    {
        var (success, error, role) = _auth.TryLogin("admin", "123456");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("admin", role);
    }

    [Fact]
    public void TryLogin_Experimenter_With_Correct_Password_Succeeds()
    {
        var (success, error, role) = _auth.TryLogin("experimenter", "123456");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("experimenter", role);
    }

    [Fact]
    public void TryLogin_With_Wrong_Password_Fails()
    {
        var (success, error, role) = _auth.TryLogin("admin", "wrong");

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Null(role);
    }

    [Fact]
    public void TryLogin_With_Empty_Password_Fails()
    {
        var (success, error, role) = _auth.TryLogin("admin", "");

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Null(role);
    }

    [Fact]
    public void TryLogin_With_Empty_Username_Fails()
    {
        var (success, error, role) = _auth.TryLogin("", "123456");

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Null(role);
    }

    [Fact]
    public void TryLogin_With_Unknown_Username_Fails()
    {
        var (success, error, role) = _auth.TryLogin("unknown", "123456");

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Null(role);
    }
}
