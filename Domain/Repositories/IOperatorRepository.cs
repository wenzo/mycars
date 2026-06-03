namespace MyCars.Domain.Repositories;

public interface IOperatorRepository
{
    Task<OperatorProfile?>               GetBySlugAsync(string slug);
    Task<OperatorProfile?>               GetByCodeAsync(string code);
    Task<OperatorProfile?>               GetByIdAsync(Guid id);
    Task<IReadOnlyList<OperatorProfile>> GetAllAsync();
    Task<OperatorProfile>                CreateAsync(OperatorProfile profile);
    Task<OperatorProfile?>               UpdateAsync(OperatorProfile profile);
    Task<bool>                           SetActiveAsync(Guid id, bool isActive);

    // App Codes
    Task<IReadOnlyList<AppCode>> GetAppCodesAsync(Guid operatorId);
    Task<AppCode>                CreateAppCodeAsync(AppCode code);
    Task<bool>                   DeleteAppCodeAsync(Guid id, Guid operatorId);
    Task<OperatorProfile?>       ResolveCodeAsync(string code);
}
