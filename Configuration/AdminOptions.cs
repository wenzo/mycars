namespace MyCars.Configuration;

public sealed class AdminOptions
{
    public string CookieName   { get; set; } = "mycars_admin";
    public int    SessionHours { get; set; } = 8;
}
