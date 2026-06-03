namespace MyCars.Domain.Models;

// Mappa la vista public_vehicle_cards
public sealed class VehicleCard
{
    public Guid    Id               { get; set; }
    public Guid    OperatorId       { get; set; }
    public string  OperatorSlug     { get; set; } = "";
    public string  OperatorCode     { get; set; } = "";
    public Guid    BranchId         { get; set; }
    public string  InternalCode     { get; set; } = "";
    public string  VehicleType      { get; set; } = "";
    public string  BrandName        { get; set; } = "";
    public string  BrandSlug        { get; set; } = "";
    public string  Model            { get; set; } = "";
    public string? Version          { get; set; }
    public string? BodyTypeName     { get; set; }
    public string  Condition        { get; set; } = "";
    public string? UsageType        { get; set; }
    public string? Fuel             { get; set; }
    public string? Transmission     { get; set; }
    public short?  RegistrationMonth { get; set; }
    public short?  RegistrationYear  { get; set; }
    public int     MileageKm        { get; set; }
    public decimal? Price           { get; set; }
    public decimal? PreviousPrice   { get; set; }
    public string  Currency         { get; set; } = "EUR";
    public bool    IsSold           { get; set; }
    public bool    ShowAsSold       { get; set; }
    public bool    ProntaConsegna   { get; set; }
    public bool    IsNuovoArrivo    { get; set; }
    public DateTimeOffset? NuovoArrivoUntil { get; set; }
    public string? Description      { get; set; }
    public string? CoverImageUrl    { get; set; }
    public string? CoverBucket      { get; set; }
    public string? CoverStoragePath { get; set; }
    public string? BranchName       { get; set; }
    public string? City             { get; set; }
    public string? Province         { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
