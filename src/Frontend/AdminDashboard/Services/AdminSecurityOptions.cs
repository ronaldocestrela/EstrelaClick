namespace EstrelaClick.Frontend.AdminDashboard.Services;

public sealed class AdminSecurityOptions
{
    public const string SectionName = "AdminSecurity";

    public string MasterUser { get; set; } = "admin@estrelaclick.com.br";
    public string MasterPassword { get; set; } = string.Empty;
    public int SessionTimeoutMinutes { get; set; } = 480;
}
