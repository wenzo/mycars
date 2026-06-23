namespace MyCars.Domain.Models;

public sealed class VehicleLead
{
    public Guid    Id                 { get; set; }
    public Guid    OperatorId         { get; set; }
    public Guid?   VehicleId          { get; set; }
    public Guid?   BranchId           { get; set; }
    public string  FullName           { get; set; } = "";
    public string? Email              { get; set; }
    public string? Phone              { get; set; }
    public string? Message            { get; set; }
    public bool    PrivacyAccepted    { get; set; }
    public bool    MarketingAccepted  { get; set; }
    public string  Source             { get; set; } = "app";
    public string  Status             { get; set; } = "new";
    public string  LeadType           { get; set; } = "info";
    public DateOnly? PreferredDate    { get; set; }
    public string? PreferredTime      { get; set; }
    public string? TrackingCode       { get; set; }
    public DateTimeOffset CreatedAt   { get; set; }
    public DateTimeOffset UpdatedAt   { get; set; }
}
