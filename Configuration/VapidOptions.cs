namespace MyCars.Configuration;

public sealed class VapidOptions
{
    public string Subject    { get; set; } = "mailto:admin@mycars.app";
    public string PublicKey  { get; set; } = "";
    public string PrivateKey { get; set; } = "";
}
