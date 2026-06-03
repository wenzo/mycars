namespace MyCars.Domain.Models;

public sealed class NewsItem
{
    public Guid    Id          { get; set; }
    public Guid    OperatorId  { get; set; }
    public Guid?   BranchId    { get; set; }
    public string  NewsType    { get; set; } = "generic";
    public string? Code        { get; set; }
    public string  Title       { get; set; } = "";
    public string  Slug        { get; set; } = "";
    public string? Excerpt     { get; set; }
    public string? Body        { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? LinkUrl     { get; set; }
    public DateTimeOffset? StartsAt    { get; set; }
    public DateTimeOffset? ExpiresAt   { get; set; }
    public bool    IsPublished  { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt    { get; set; }
    public DateTimeOffset UpdatedAt    { get; set; }
}
