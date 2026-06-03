namespace MyCars.Domain.Models;

public sealed class OperatorUser
{
    public Guid    Id           { get; set; }
    public Guid    OperatorId   { get; set; }
    public string  Email        { get; set; } = "";
    public string  PasswordHash { get; set; } = "";
    public string  FullName     { get; set; } = "";
    public bool    IsActive     { get; set; } = true;
    public DateTimeOffset  CreatedAt   { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
