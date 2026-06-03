namespace MyCars.Configuration;

public sealed class JwtOptions
{
    public string Issuer          { get; set; } = "MyCars";
    public string Audience        { get; set; } = "MyCars.Client";
    public string Key             { get; set; } = "";
    public int    ExpiryMinutes   { get; set; } = 1440;
    public int    RefreshExpiryDays { get; set; } = 30;
}
