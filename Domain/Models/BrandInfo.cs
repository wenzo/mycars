namespace MyCars.Domain.Models;

public sealed class BrandInfo
{
    public Guid   Id   { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}
