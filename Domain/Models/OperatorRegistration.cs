namespace MyCars.Domain.Models;

public sealed class OperatorRegistration
{
    public Guid    Id            { get; set; }
    public string  BusinessName  { get; set; } = "";
    public string? VatNumber     { get; set; }
    public string  Email         { get; set; } = "";
    public string? Phone         { get; set; }
    public string  ContactPerson { get; set; } = "";
    public string? Address       { get; set; }
    public string? City          { get; set; }
    public string? Province      { get; set; }
    public string? Website       { get; set; }
    public string? Notes         { get; set; }
    public string  Status        { get; set; } = "pending"; // pending | approved | rejected
    public DateTimeOffset? ReviewedAt  { get; set; }
    public string? ReviewNotes   { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
