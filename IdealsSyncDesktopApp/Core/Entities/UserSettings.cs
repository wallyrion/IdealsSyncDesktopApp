namespace IdealsSyncDesktopApp.Core;

public class UserSettings
{
    public string? Email { get; set; }
    public string? SyncPath { get; set; }
    public int OperationDelay { get; set; } = 500;
}