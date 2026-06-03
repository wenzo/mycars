namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestScheduledPushRepository : IScheduledPushRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestScheduledPushRepository(ISupabaseRestClient db) => _db = db;

    public async Task<ScheduledPushNotification> CreateAsync(ScheduledPushNotification item)
    {
        item.Id        = Guid.NewGuid();
        item.CreatedAt = DateTimeOffset.UtcNow;

        var result = await _db.InsertAsync<ScheduledPushNotification>(
            "scheduled_push_notifications", new
            {
                id           = item.Id,
                operator_id  = item.OperatorId,
                news_id      = item.NewsId,
                title        = item.Title,
                body         = item.Body,
                image_url    = item.ImageUrl,
                topic        = item.Topic,
                scheduled_at = item.ScheduledAt,
                created_at   = item.CreatedAt,
            });
        return result ?? item;
    }

    public async Task<IReadOnlyList<ScheduledPushNotification>> GetPendingAsync()
    {
        var now = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("o"));
        return await _db.SelectAsync<ScheduledPushNotification>(
            "scheduled_push_notifications",
            $"sent_at=is.null&error=is.null&scheduled_at=lte.{now}",
            order: "scheduled_at.asc",
            limit: 100);
    }

    public async Task MarkSentAsync(Guid id) =>
        await _db.UpdateAsync<ScheduledPushNotification>(
            "scheduled_push_notifications", $"id=eq.{id}",
            new { sent_at = DateTimeOffset.UtcNow });

    public async Task MarkErrorAsync(Guid id, string error) =>
        await _db.UpdateAsync<ScheduledPushNotification>(
            "scheduled_push_notifications", $"id=eq.{id}",
            new { error });
}
