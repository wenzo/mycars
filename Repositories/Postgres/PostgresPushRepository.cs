using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresPushRepository : IPushRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresPushRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectCols =
        "id, operator_id, vehicle_id, user_email, endpoint, p256dh, auth, device_type, topic_general, topic_vehicles, topic_news";

    public async Task UpsertAsync(PushSubscription subscription)
    {
        const string sql = """
            INSERT INTO public.push_subscriptions
                (operator_id, vehicle_id, user_email, endpoint, p256dh, auth, device_type,
                 topic_general, topic_vehicles, topic_news, last_active_at)
            VALUES
                (@OperatorId, @VehicleId, @UserEmail, @Endpoint, @P256dh, @Auth, @DeviceType,
                 @TopicGeneral, @TopicVehicles, @TopicNews, now())
            ON CONFLICT (endpoint) DO UPDATE
                SET last_active_at = now(),
                    operator_id    = EXCLUDED.operator_id,
                    vehicle_id     = COALESCE(EXCLUDED.vehicle_id,  push_subscriptions.vehicle_id),
                    user_email     = COALESCE(EXCLUDED.user_email,  push_subscriptions.user_email),
                    topic_general  = EXCLUDED.topic_general,
                    topic_vehicles = EXCLUDED.topic_vehicles,
                    topic_news     = EXCLUDED.topic_news
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, subscription);
    }

    public async Task<IReadOnlyList<PushSubscription>> GetAllAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? $"SELECT {SelectCols} FROM public.push_subscriptions WHERE operator_id = @id"
            : $"SELECT {SelectCols} FROM public.push_subscriptions";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<PushSubscription>(sql,
            operatorId.HasValue ? new { id = operatorId } : null)).AsList();
    }

    public async Task<IReadOnlyList<PushSubscription>> GetByTopicAsync(string topic, Guid operatorId)
    {
        var col = topic switch
        {
            "news"     => "topic_news",
            "vehicles" => "topic_vehicles",
            _          => "topic_general",
        };
        var sql = $"SELECT {SelectCols} FROM public.push_subscriptions WHERE operator_id = @operatorId AND {col} = true";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<PushSubscription>(sql, new { operatorId })).AsList();
    }

    public async Task<IReadOnlyList<PushSubscription>> GetByVehicleAsync(Guid vehicleId)
    {
        var sql = $"SELECT {SelectCols} FROM public.push_subscriptions WHERE vehicle_id = @vehicleId";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<PushSubscription>(sql, new { vehicleId })).AsList();
    }

    public async Task<IReadOnlyList<PushSubscription>> GetByEmailAsync(string email, Guid operatorId)
    {
        var sql = $"SELECT {SelectCols} FROM public.push_subscriptions WHERE operator_id = @operatorId AND lower(user_email) = lower(@email)";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<PushSubscription>(sql, new { email, operatorId })).AsList();
    }

    public async Task DeleteAsync(string endpoint)
    {
        const string sql = "DELETE FROM public.push_subscriptions WHERE endpoint = @endpoint";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new { endpoint });
    }

    public async Task<long> CountAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? "SELECT COUNT(*) FROM public.push_subscriptions WHERE operator_id = @id"
            : "SELECT COUNT(*) FROM public.push_subscriptions";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(sql,
            operatorId.HasValue ? new { id = operatorId } : null);
    }
}
