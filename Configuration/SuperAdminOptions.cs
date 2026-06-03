namespace MyCars.Configuration;

public sealed class SuperAdminOptions
{
    public string Username     { get; set; } = "superadmin";
    public string PasswordHash { get; set; } = "";
    public string CookieName   { get; set; } = "mycars_superadmin";
    public int    SessionHours { get; set; } = 8;
}
