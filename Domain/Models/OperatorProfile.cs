namespace MyCars.Domain.Models;

public sealed class OperatorProfile
{
    public Guid    Id               { get; set; }
    public string  BusinessName     { get; set; } = "";
    public string  Slug             { get; set; } = "";
    public string  PublicCode       { get; set; } = "";
    public string? VatNumber        { get; set; }
    public string? FiscalCode       { get; set; }
    public string? WebsiteUrl       { get; set; }
    public string? Phone            { get; set; }
    public string? Email            { get; set; }
    public string? WhatsappNumber   { get; set; }
    public string? PrimaryColor     { get; set; }
    public string? SecondaryColor   { get; set; }
    public string? AccentColor      { get; set; }
    public string? LogoUrl           { get; set; }
    public string? CoverImageUrl    { get; set; }
    public bool    IsActive         { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
