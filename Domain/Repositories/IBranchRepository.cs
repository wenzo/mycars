namespace MyCars.Domain.Repositories;

public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> GetByOperatorAsync(Guid operatorId);
    Task<Branch?>               GetByIdAsync(Guid id, Guid operatorId);
    Task<Branch>                CreateAsync(Branch branch);
    Task<Branch?>               UpdateAsync(Branch branch);
    Task<bool>                  DeleteAsync(Guid id, Guid operatorId);
}
