namespace MyCars.Domain.Repositories;

public interface IScheduledPushRepository
{
    Task<ScheduledPushNotification>            CreateAsync(ScheduledPushNotification item);
    Task<IReadOnlyList<ScheduledPushNotification>> GetPendingAsync();
    Task MarkSentAsync(Guid id);
    Task MarkErrorAsync(Guid id, string error);
}
