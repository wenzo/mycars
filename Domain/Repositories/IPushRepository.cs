namespace MyCars.Domain.Repositories;

public interface IPushRepository
{
    Task UpsertAsync(PushSubscription subscription);
    Task<IReadOnlyList<PushSubscription>> GetAllAsync(Guid? operatorId = null);
    Task<IReadOnlyList<PushSubscription>> GetByVehicleAsync(Guid vehicleId);
    Task<IReadOnlyList<PushSubscription>> GetByEmailAsync(string email, Guid operatorId);
    Task<IReadOnlyList<PushSubscription>> GetByTopicAsync(string topic, Guid operatorId);
    Task DeleteAsync(string endpoint);
    Task<long> CountAsync(Guid? operatorId = null);
}
