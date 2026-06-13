namespace MyCars.Domain.Models;

public sealed class OperatorProfile
{
    public Guid    Id               { get; set; }
    public string  BusinessName     { get; set; } = "";
    public string  Slug             { get; set; } = "";
    public string  PublicCode       { get; set; } = "";
    public string? VatNumber        { get; set; }
    public string? FiscalCode       { get; set; }
    public string? ReaNumber        { get; set; }
    public string? WebsiteUrl       { get; set; }
    public string? Phone            { get; set; }
    public string? Email            { get; set; }
    public string? WhatsappNumber   { get; set; }
    public string? Address          { get; set; }
    public string? City             { get; set; }
    public string? Province         { get; set; }
    public string? ZipCode          { get; set; }
    public double? Latitude         { get; set; }
    public double? Longitude        { get; set; }
    public string? PrimaryColor     { get; set; }
    public string? SecondaryColor   { get; set; }
    public string? AccentColor      { get; set; }
    public string? LogoUrl          { get; set; }
    public string? CoverImageUrl    { get; set; }
    public string? Tagline          { get; set; }
    public bool    IsActive         { get; set; }
    // Modulo noleggio
    public bool    RentalModuleEnabled      { get; set; }
    public bool    RentalPhotosEnabled      { get; set; }
    public bool    RentalContractPdfEnabled { get; set; }
    public bool    RentalShowPrices         { get; set; }
    // SMTP per-operatore (sovrascrive appsettings se impostato)
    public string? SmtpHost      { get; set; }
    public int?    SmtpPort      { get; set; }
    public bool    SmtpUseSsl    { get; set; } = true;
    public string? SmtpUsername  { get; set; }
    public string? SmtpPassword  { get; set; }
    public string? SmtpFromEmail { get; set; }
    public string? SmtpFromName  { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
