namespace MyCars.Domain.Repositories;

public interface IScheduledPushRepository
{
    Task<ScheduledPushNotification>               CreateAsync(ScheduledPushNotification item);
    Task<IReadOnlyList<ScheduledPushNotification>> GetPendingAsync();
    Task<IReadOnlyList<ScheduledPushNotification>> GetByOperatorAsync(Guid operatorId, int limit = 50);
    Task<bool>                                     DeleteAsync(Guid id, Guid operatorId);
    Task MarkSentAsync(Guid id);
    Task MarkErrorAsync(Guid id, string error);
}
