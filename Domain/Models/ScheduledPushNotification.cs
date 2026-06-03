namespace MyCars.Domain.Models;

public sealed class ScheduledPushNotification
{
    public Guid    Id          { get; set; }
    public Guid    OperatorId  { get; set; }
    public Guid?   NewsId      { get; set; }
    public string  Title       { get; set; } = "";
    public string  Body        { get; set; } = "";
    public string? ImageUrl    { get; set; }
    public string  Topic       { get; set; } = "general"; // "general" | "news" | "vehicles"
    public DateTimeOffset  ScheduledAt { get; set; }
    public DateTimeOffset? SentAt      { get; set; }
    public string?         Error       { get; set; }
    public DateTimeOffset  CreatedAt   { get; set; }
}
