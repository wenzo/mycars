namespace MyCars.Domain.Models;

public sealed class RentalPhoto
{
    public Guid   Id         { get; set; }
    public Guid   RentalId   { get; set; }
    public Guid   OperatorId { get; set; }
    public string Phase      { get; set; } = "departure";  // departure | return
    public string Url        { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
