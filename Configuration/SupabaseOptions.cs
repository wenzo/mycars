namespace MyCars.Configuration;

public sealed class SupabaseOptions
{
    public string Url              { get; set; } = "";
    public string RestBasePath     { get; set; } = "/rest/v1";
    public string StorageBasePath  { get; set; } = "/storage/v1";
    public string AnonKey          { get; set; } = "";
    public string ServiceRoleKey   { get; set; } = "";
    public SupabaseHttpOptions HttpClient { get; set; } = new();
}

public sealed class SupabaseHttpOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount     { get; set; } = 2;
}
