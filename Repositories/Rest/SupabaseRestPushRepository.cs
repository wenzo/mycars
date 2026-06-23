namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestPushRepository : IPushRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestPushRepository(ISupabaseRestClient db) => _db = db;

    private const string Cols = "id,operator_id,vehicle_id,user_email,endpoint,p256dh,auth,device_type,topic_general,topic_vehicles,topic_news";

    public Task UpsertAsync(PushSubscription subscription)
        => _db.UpsertAsync("push_subscriptions", new
        {
            operator_id    = subscription.OperatorId,
            vehicle_id     = subscription.VehicleId,
            user_email     = subscription.UserEmail,
            endpoint       = subscription.Endpoint,
            p256dh         = subscription.P256dh,
            auth           = subscription.Auth,
            device_type    = subscription.DeviceType,
            topic_general  = subscription.TopicGeneral,
            topic_vehicles = subscription.TopicVehicles,
            topic_news     = subscription.TopicNews,
            last_active_at = DateTimeOffset.UtcNow,
        }, onConflict: "endpoint");

    public Task<IReadOnlyList<PushSubscription>> GetAllAsync(Guid? operatorId = null)
    {
        var filter = operatorId.HasValue ? $"operator_id=eq.{operatorId}" : null;
        return _db.SelectAsync<PushSubscription>("push_subscriptions", filter, select: Cols);
    }

    public Task<IReadOnlyList<PushSubscription>> GetByTopicAsync(string topic, Guid operatorId)
    {
        var col = topic switch
        {
            "news"     => "topic_news",
            "vehicles" => "topic_vehicles",
            _          => "topic_general",
        };
        return _db.SelectAsync<PushSubscription>("push_subscriptions",
            $"operator_id=eq.{operatorId}&{col}=eq.true", select: Cols);
    }

    public Task<IReadOnlyList<PushSubscription>> GetByVehicleAsync(Guid vehicleId)
        => _db.SelectAsync<PushSubscription>("push_subscriptions",
            $"vehicle_id=eq.{vehicleId}", select: Cols);

    public Task<IReadOnlyList<PushSubscription>> GetByEmailAsync(string email, Guid operatorId)
        => _db.SelectAsync<PushSubscription>("push_subscriptions",
            $"operator_id=eq.{operatorId}&user_email=ilike.{Uri.EscapeDataString(email)}", select: Cols);

    public Task DeleteAsync(string endpoint)
        => _db.DeleteAsync("push_subscriptions", $"endpoint=eq.{Uri.EscapeDataString(endpoint)}");

    public Task<long> CountAsync(Guid? operatorId = null)
    {
        var filter = operatorId.HasValue ? $"operator_id=eq.{operatorId}" : null;
        return _db.CountAsync("push_subscriptions", filter);
    }
}
