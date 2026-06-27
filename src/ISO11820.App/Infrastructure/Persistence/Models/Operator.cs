namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class Operator
{
    public long Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "experimenter";

    public string CreatedAt { get; set; } = string.Empty;
}