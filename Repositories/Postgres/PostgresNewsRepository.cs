using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresNewsRepository : INewsRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresNewsRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<NewsItem>> GetPublishedAsync(
        Guid operatorId, string? newsType, PageRequest page)
    {
        var where = "operator_id = @operatorId AND is_published = true" +
                    (newsType is not null ? " AND news_type = @newsType::public.news_type" : "");

        var countSql = $"SELECT COUNT(*) FROM public.news_items WHERE {where}";
        var itemsSql = $"""
            SELECT id, operator_id, branch_id,
                   news_type::text AS news_type,
                   code, title, slug, excerpt, body,
                   link_url, starts_at, expires_at,
                   is_published, published_at, created_at, updated_at
            FROM public.news_items
            WHERE {where}
            ORDER BY published_at DESC NULLS LAST
            LIMIT @pageSize OFFSET @offset
            """;
        var param = new { operatorId, newsType, pageSize = page.PageSize, offset = page.Page * page.PageSize };
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, param);
        var items = (await conn.QueryAsync<NewsItem>(itemsSql, param)).AsList();
        return new PagedResult<NewsItem>(items, total);
    }

    public async Task<NewsItem?> GetByIdAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            SELECT id, operator_id, branch_id,
                   news_type::text AS news_type,
                   code, title, slug, excerpt, body,
                   link_url, starts_at, expires_at,
                   is_published, published_at, created_at, updated_at
            FROM public.news_items
            WHERE id = @id AND operator_id = @operatorId
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<NewsItem>(sql, new { id, operatorId });
    }

    public async Task<PagedResult<NewsItem>> GetByOperatorAsync(Guid operatorId, PageRequest page)
    {
        const string countSql =
            "SELECT COUNT(*) FROM public.news_items WHERE operator_id = @operatorId";
        const string itemsSql = """
            SELECT id, operator_id, branch_id,
                   news_type::text AS news_type,
                   code, title, slug, excerpt,
                   is_published, published_at, created_at, updated_at
            FROM public.news_items
            WHERE operator_id = @operatorId
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset
            """;
        var param = new { operatorId, pageSize = page.PageSize, offset = page.Page * page.PageSize };
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, param);
        var items = (await conn.QueryAsync<NewsItem>(itemsSql, param)).AsList();
        return new PagedResult<NewsItem>(items, total);
    }

    public async Task<PagedResult<NewsItem>> GetAllAsync(
        Guid operatorId, PageRequest page, string? newsType = null, bool? isPublished = null)
    {
        var parts = new List<string> { "operator_id = @operatorId" };
        var p     = new DynamicParameters();
        p.Add("operatorId", operatorId);

        if (!string.IsNullOrEmpty(newsType))
            { parts.Add("news_type = @newsType::public.news_type"); p.Add("newsType", newsType); }
        if (isPublished.HasValue)
            { parts.Add("is_published = @isPublished"); p.Add("isPublished", isPublished.Value); }

        p.Add("pageSize", page.PageSize);
        p.Add("offset",   page.Page * page.PageSize);

        var where    = "WHERE " + string.Join(" AND ", parts);
        var countSql = $"SELECT COUNT(*) FROM public.news_items {where}";
        var itemsSql = $"""
            SELECT id, operator_id, branch_id,
                   news_type::text AS news_type,
                   title, slug, excerpt,
                   is_published, published_at, starts_at, expires_at,
                   created_at, updated_at
            FROM public.news_items
            {where}
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset
            """;
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, p);
        var items = (await conn.QueryAsync<NewsItem>(itemsSql, p)).AsList();
        return new PagedResult<NewsItem>(items, total);
    }

    public async Task<int> CountPublishedAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? "SELECT COUNT(*) FROM public.news_items WHERE operator_id = @id AND is_published = true"
            : "SELECT COUNT(*) FROM public.news_items WHERE is_published = true";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, operatorId.HasValue ? new { id = operatorId } : null);
    }

    public async Task<NewsItem> CreateAsync(NewsItem item)
    {
        item.Id        = Guid.NewGuid();
        item.CreatedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            INSERT INTO public.news_items
                (id, operator_id, branch_id, news_type, code, title, slug, excerpt, body,
                 cover_image_url, link_url, starts_at, expires_at,
                 is_published, published_at, created_at, updated_at)
            VALUES
                (@Id, @OperatorId, @BranchId, @NewsType::public.news_type, @Code, @Title, @Slug, @Excerpt, @Body,
                 @CoverImageUrl, @LinkUrl, @StartsAt, @ExpiresAt,
                 @IsPublished, @PublishedAt, @CreatedAt, @UpdatedAt)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, item);
        return item;
    }

    public async Task<NewsItem?> UpdateAsync(NewsItem item)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            UPDATE public.news_items SET
                branch_id       = @BranchId,
                news_type       = @NewsType::public.news_type,
                code            = @Code,
                title           = @Title,
                slug            = @Slug,
                excerpt         = @Excerpt,
                body            = @Body,
                cover_image_url = @CoverImageUrl,
                link_url        = @LinkUrl,
                starts_at       = @StartsAt,
                expires_at      = @ExpiresAt,
                is_published    = @IsPublished,
                published_at    = @PublishedAt,
                updated_at      = @UpdatedAt
            WHERE id = @Id AND operator_id = @OperatorId
            """;
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, item);
        return rows > 0 ? item : null;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        const string sql =
            "DELETE FROM public.news_items WHERE id = @id AND operator_id = @operatorId";
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { id, operatorId });
        return rows > 0;
    }
}
