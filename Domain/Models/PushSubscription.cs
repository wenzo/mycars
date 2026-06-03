namespace MyCars.Domain.Models;

public sealed class PushSubscription
{
    public Guid    Id          { get; set; }
    public Guid?   OperatorId  { get; set; }
    public Guid?   VehicleId   { get; set; }
    public string  Endpoint    { get; set; } = "";
    public string  P256dh      { get; set; } = "";
    public string  Auth        { get; set; } = "";
    public string  DeviceType  { get; set; } = "web";
    public string? UserEmail   { get; set; }
    public bool TopicGeneral  { get; set; } = true;
    public bool TopicVehicles { get; set; } = true;
    public bool TopicNews     { get; set; } = true;
    public DateTimeOffset CreatedAt    { get; set; }
    public DateTimeOffset LastActiveAt { get; set; }
}
