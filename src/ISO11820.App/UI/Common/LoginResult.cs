namespace ISO11820.App.UI.Common;

/// <summary>
/// Carries login outcome from <see cref="UI.Forms.LoginForm"/> to <see cref="UI.Forms.MainForm"/>.
/// </summary>
public sealed record LoginResult(string Role, string OperatorName);
