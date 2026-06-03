namespace MyCars.Domain.Models;

public sealed class Branch
{
    public Guid    Id              { get; set; }
    public Guid    OperatorId      { get; set; }
    public string  Name            { get; set; } = "";
    public string  Slug            { get; set; } = "";
    public string? LegalName       { get; set; }
    public string? Address         { get; set; }
    public string? ZipCode         { get; set; }
    public string? City            { get; set; }
    public string? Province        { get; set; }
    public string  CountryCode     { get; set; } = "IT";
    public double? Latitude        { get; set; }
    public double? Longitude       { get; set; }
    public string? Phone           { get; set; }
    public string? Email           { get; set; }
    public string? WhatsappNumber  { get; set; }
    public bool    IsLegalAddress   { get; set; }
    public bool    IsActive        { get; set; }
    public int     SortOrder       { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
