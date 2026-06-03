using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresScheduledPushRepository : IScheduledPushRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresScheduledPushRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<ScheduledPushNotification> CreateAsync(ScheduledPushNotification item)
    {
        item.Id        = Guid.NewGuid();
        item.CreatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            INSERT INTO public.scheduled_push_notifications
                (id, operator_id, news_id, title, body, image_url, topic, scheduled_at, created_at)
            VALUES
                (@Id, @OperatorId, @NewsId, @Title, @Body, @ImageUrl, @Topic, @ScheduledAt, @CreatedAt)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, item);
        return item;
    }

    public async Task<IReadOnlyList<ScheduledPushNotification>> GetPendingAsync()
    {
        const string sql = """
            SELECT id, operator_id, news_id, title, body, image_url, topic, scheduled_at, created_at
            FROM public.scheduled_push_notifications
            WHERE sent_at IS NULL AND error IS NULL AND scheduled_at <= now()
            ORDER BY scheduled_at
            LIMIT 100
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<ScheduledPushNotification>(sql)).AsList();
    }

    public async Task<IReadOnlyList<ScheduledPushNotification>> GetByOperatorAsync(Guid operatorId, int limit = 50)
    {
        const string sql = """
            SELECT id, operator_id, news_id, title, body, image_url, topic,
                   scheduled_at, sent_at, error, created_at
            FROM public.scheduled_push_notifications
            WHERE operator_id = @operatorId
            ORDER BY scheduled_at DESC
            LIMIT @limit
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<ScheduledPushNotification>(sql, new { operatorId, limit })).AsList();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            DELETE FROM public.scheduled_push_notifications
            WHERE id = @id AND operator_id = @operatorId AND sent_at IS NULL
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, operatorId }) > 0;
    }

    public async Task MarkSentAsync(Guid id)
    {
        const string sql = "UPDATE public.scheduled_push_notifications SET sent_at = now() WHERE id = @id";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new { id });
    }

    public async Task MarkErrorAsync(Guid id, string error)
    {
        const string sql = "UPDATE public.scheduled_push_notifications SET error = @error WHERE id = @id";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new { id, error });
    }
}
