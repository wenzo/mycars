namespace MyCars.Domain.Models;

public sealed class Rental
{
    public Guid    Id               { get; set; }
    public Guid    OperatorId       { get; set; }
    public Guid    VehicleId        { get; set; }

    // Cliente
    public string  CustomerName     { get; set; } = "";
    public string? CustomerPhone    { get; set; }
    public string? CustomerLicense  { get; set; }
    public string? CustomerFiscalCode { get; set; }

    // Date
    public DateOnly StartDate       { get; set; }
    public DateOnly PlannedEndDate  { get; set; }
    public DateOnly? ActualEndDate  { get; set; }

    // Stato veicolo
    public int?    KmDeparture      { get; set; }
    public int?    KmReturn         { get; set; }
    public string? FuelDeparture    { get; set; }  // full | three_quarters | half | quarter | empty
    public string? FuelReturn       { get; set; }

    // Economico (semi-strutturato, tutto opzionale)
    public decimal? AgreedPrice     { get; set; }
    public decimal? DepositAmount   { get; set; }
    public bool    DepositReturned  { get; set; }
    public string? PaymentMethod    { get; set; }  // cash | pos | transfer
    public bool    IsPaid           { get; set; }

    // Stato workflow
    public string  Status           { get; set; } = "booked";  // booked | active | closed | cancelled

    public string? Notes            { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Campi calcolati / join (non persistiti, popolati on demand)
    public string? VehicleBrand     { get; set; }
    public string? VehicleModel     { get; set; }
    public string? VehicleTarga     { get; set; }
}
