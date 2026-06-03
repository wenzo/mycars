namespace MyCars.Domain.Models;

public sealed class VehicleImage
{
    public Guid   Id         { get; set; }
    public Guid   VehicleId  { get; set; }
    public Guid   OperatorId { get; set; }
    public string Url        { get; set; } = "";
    public int    SortOrder  { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
