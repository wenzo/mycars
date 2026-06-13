namespace MyCars.Domain.Models;

public sealed class Vehicle
{
    public Guid    Id               { get; set; }
    public Guid    OperatorId       { get; set; }
    public Guid    BranchId         { get; set; }
    public Guid?   DepartmentId     { get; set; }
    public string  InternalCode     { get; set; } = "";
    public string? ExternalCode     { get; set; }
    public string? Vin              { get; set; }
    public string? Targa            { get; set; }
    public string  VehicleType      { get; set; } = "";
    public Guid    BrandId          { get; set; }
    public string? BrandName        { get; set; }
    public string  Model            { get; set; } = "";
    public string? Version          { get; set; }
    public Guid?   BodyTypeId       { get; set; }
    public string? UsageType        { get; set; }
    public string  Condition        { get; set; } = "usato";
    public string? Fuel             { get; set; }
    public string? Transmission     { get; set; }
    public int?    EngineCapacityCc { get; set; }
    public int?    HorsepowerCv     { get; set; }
    public int?    PowerKw          { get; set; }
    public short?  RegistrationMonth { get; set; }
    public short?  RegistrationYear  { get; set; }
    public int     MileageKm        { get; set; }
    public short?  Doors            { get; set; }
    public short?  Seats            { get; set; }
    public string? Color            { get; set; }
    public string? EmissionClass    { get; set; }
    public bool    HandicapAccessible { get; set; }
    public bool    VatDeductible    { get; set; }
    public bool    Damaged          { get; set; }
    public bool    Imported         { get; set; }
    public bool    ForSale          { get; set; } = true;
    public bool    ForRental        { get; set; }
    public bool    RentalOnly       { get; set; }
    public decimal? RentalPrice     { get; set; }
    public decimal? RentalWeeklyPrice  { get; set; }
    public decimal? RentalWeekendPrice { get; set; }
    public string? Description      { get; set; }
    public string  Equipment        { get; set; } = "[]";
    public decimal? Price           { get; set; }
    public decimal? PreviousPrice   { get; set; }
    public string  Currency         { get; set; } = "EUR";
    public DateOnly? ListingDate    { get; set; }
    public bool    IsSold           { get; set; }
    public bool    ShowAsSold       { get; set; }
    public DateTimeOffset? SoldAt   { get; set; }
    public bool    ProntaConsegna   { get; set; }
    public bool    IsNuovoArrivo    { get; set; }
    public DateTimeOffset? NuovoArrivoUntil { get; set; }
    public bool    IsPublished      { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int     SortOrder        { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? CoverImageUrl   { get; set; }
}
