using ISO11820.App.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace ISO11820.App.Features.Auth;

/// <summary>
/// 登录认证协调器，通过数据库验证用户名和密码
/// </summary>
public sealed class AuthCoordinator
{
    private readonly DbHelper _dbHelper;

    public AuthCoordinator(DbHelper dbHelper)
    {
        _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
    }

    /// <summary>
    /// 验证登录凭据
    /// </summary>
    /// <param name="username">用户名（admin 或 experimenter）</param>
    /// <param name="password">明文密码</param>
    /// <returns>(成功, 错误消息, 角色)</returns>
    public (bool Success, string? ErrorMessage, string? Role) TryLogin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return (false, "用户名不能为空", null);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "密码不能为空", null);
        }

        var hash = HashPassword(password);

        using var connection = _dbHelper.CreateConnection();
        var sql = "SELECT role FROM operators WHERE username = @username AND pwd = @password";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", hash);

        var result = command.ExecuteScalar();
        if (result is string role)
        {
            return (true, null, role);
        }

        return (false, "密码错误，请重新输入", null);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
