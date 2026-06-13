namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestNewsRepository : INewsRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestNewsRepository(ISupabaseRestClient db) => _db = db;

    public async Task<PagedResult<NewsItem>> GetPublishedAsync(
        Guid operatorId, string? newsType, PageRequest page)
    {
        var f = $"operator_id=eq.{operatorId}&is_published=eq.true";
        if (!string.IsNullOrEmpty(newsType)) f += $"&news_type=eq.{newsType}";

        var (items, total) = await _db.SelectWithCountAsync<NewsItem>(
            "news_items", f, order: "published_at.desc",
            limit: page.PageSize, offset: page.Page * page.PageSize);
        return new PagedResult<NewsItem>(items, total);
    }

    public Task<NewsItem?> GetByIdAsync(Guid id, Guid operatorId)
        => _db.SelectOneAsync<NewsItem>("news_items",
            $"id=eq.{id}&operator_id=eq.{operatorId}");

    public async Task<PagedResult<NewsItem>> GetByOperatorAsync(Guid operatorId, PageRequest page)
    {
        var f = $"operator_id=eq.{operatorId}";
        var total = await _db.CountAsync("news_items", f);
        var items = await _db.SelectAsync<NewsItem>(
            "news_items", f, order: "created_at.desc",
            limit: page.PageSize, offset: page.Page * page.PageSize);
        return new PagedResult<NewsItem>(items, total);
    }

    public async Task<PagedResult<NewsItem>> GetAllAsync(
        Guid operatorId, PageRequest page, string? newsType = null, bool? isPublished = null)
    {
        var parts = new List<string> { $"operator_id=eq.{operatorId}" };
        if (!string.IsNullOrEmpty(newsType)) parts.Add($"news_type=eq.{newsType}");
        if (isPublished.HasValue) parts.Add($"is_published=eq.{isPublished.Value.ToString().ToLower()}");

        var f     = string.Join("&", parts);
        var total = await _db.CountAsync("news_items", f);
        var items = await _db.SelectAsync<NewsItem>(
            "news_items", f, order: "created_at.desc",
            limit: page.PageSize, offset: page.Page * page.PageSize);
        return new PagedResult<NewsItem>(items, total);
    }

    public Task<int> CountPublishedAsync(Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&is_published=eq.true"
            : "is_published=eq.true";
        return _db.CountAsync("news_items", f).ContinueWith(t => (int)t.Result);
    }

    public async Task<NewsItem> CreateAsync(NewsItem item)
    {
        item.Id        = Guid.NewGuid();
        item.CreatedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        // Payload esplicito: news_items ha cover_media_id (uuid), NON cover_image_url.
        var result = await _db.InsertAsync<NewsItem>("news_items", new
        {
            id           = item.Id,
            operator_id  = item.OperatorId,
            branch_id    = item.BranchId,
            news_type    = item.NewsType,
            code         = item.Code,
            title        = item.Title,
            slug         = item.Slug,
            excerpt      = item.Excerpt,
            body         = item.Body,
            link_url     = item.LinkUrl,
            starts_at    = item.StartsAt,
            expires_at   = item.ExpiresAt,
            is_published = item.IsPublished,
            published_at = item.PublishedAt,
            created_at   = item.CreatedAt,
            updated_at   = item.UpdatedAt,
        });
        return result ?? item;
    }

    public async Task<NewsItem?> UpdateAsync(NewsItem item)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;
        return await _db.UpdateAsync<NewsItem>(
            "news_items",
            $"id=eq.{item.Id}&operator_id=eq.{item.OperatorId}",
            new
            {
                news_type    = item.NewsType,
                code         = item.Code,
                title        = item.Title,
                slug         = item.Slug,
                excerpt      = item.Excerpt,
                body         = item.Body,
                link_url     = item.LinkUrl,
                starts_at    = item.StartsAt,
                expires_at   = item.ExpiresAt,
                is_published = item.IsPublished,
                published_at = item.PublishedAt,
                updated_at   = item.UpdatedAt,
            });
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        try
        {
            await _db.DeleteAsync("news_items", $"id=eq.{id}&operator_id=eq.{operatorId}");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
