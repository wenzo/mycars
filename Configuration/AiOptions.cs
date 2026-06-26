namespace MyCars.Configuration;

public sealed class AiOptions
{
    public string Provider { get; set; } = "Groq";
    public Dictionary<string, AiProviderOptions> Providers { get; set; } = new();
}

public sealed class AiProviderOptions
{
    public string ApiKey    { get; set; } = "";
    public string Model     { get; set; } = "";
    public string BaseUrl   { get; set; } = "";
    public int    MaxTokens { get; set; } = 1024;
}
