namespace MyCars.Infrastructure.Http;

public interface ISupabaseRestClient
{
    Task<IReadOnlyList<T>> SelectAsync<T>(
        string table,
        string? filter   = null,
        string? select   = null,
        string? order    = null,
        int?    limit    = null,
        int?    offset   = null);

    Task<T?> SelectOneAsync<T>(
        string table,
        string  filter,
        string? select = null);

    Task<T?> InsertAsync<T>(string table, object payload, string? select = null);

    Task UpsertAsync(string table, object payload, string onConflict);

    Task<T?> UpdateAsync<T>(string table, string filter, object payload, string? select = null);

    Task DeleteAsync(string table, string filter);

    Task<long> CountAsync(string table, string? filter = null);

    Task<(IReadOnlyList<T> Items, long TotalCount)> SelectWithCountAsync<T>(
        string table,
        string? filter = null,
        string? select = null,
        string? order  = null,
        int?    limit  = null,
        int?    offset = null);
}
