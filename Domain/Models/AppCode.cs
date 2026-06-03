namespace MyCars.Domain.Models;

public sealed class AppCode
{
    public Guid    Id          { get; set; }
    public Guid    OperatorId  { get; set; }
    public string  Code        { get; set; } = "";
    public string? Label       { get; set; }
    public bool    IsPrimary   { get; set; }
    public bool    IsActive    { get; set; } = true;
    public DateTimeOffset? ExpiresAt { get; set; }
    public int?    MaxUses     { get; set; }
    public int     UseCount    { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
