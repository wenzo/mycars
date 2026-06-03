namespace MyCars.Domain.Repositories;

public interface INewsRepository
{
    Task<PagedResult<NewsItem>>    GetPublishedAsync(Guid operatorId, string? newsType, PageRequest page);
    Task<NewsItem?>                GetByIdAsync(Guid id, Guid operatorId);

    // Admin / Portale
    Task<PagedResult<NewsItem>>    GetByOperatorAsync(Guid operatorId, PageRequest page);
    Task<int>                      CountPublishedAsync(Guid? operatorId = null);
    Task<PagedResult<NewsItem>>    GetAllAsync(Guid operatorId, PageRequest page, string? newsType = null, bool? isPublished = null);
    Task<NewsItem>                 CreateAsync(NewsItem item);
    Task<NewsItem?>                UpdateAsync(NewsItem item);
    Task<bool>                     DeleteAsync(Guid id, Guid operatorId);
}
