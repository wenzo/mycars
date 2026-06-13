namespace MyCars.Domain.Models;

public sealed class Department
{
    public Guid    Id                { get; set; }
    public Guid    OperatorId        { get; set; }
    public Guid?   BranchId          { get; set; }
    public string  Name              { get; set; } = "";
    public string? Description       { get; set; }
    public string? ResponsibleName   { get; set; }
    public string? Phone             { get; set; }
    public string? Email             { get; set; }
    public int     SortOrder         { get; set; }
    public bool    IsActive          { get; set; } = true;
    public DateTimeOffset CreatedAt  { get; set; }
    public DateTimeOffset UpdatedAt  { get; set; }
}
