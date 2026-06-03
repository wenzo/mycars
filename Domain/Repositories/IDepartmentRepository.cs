namespace MyCars.Domain.Repositories;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetByOperatorAsync(Guid operatorId);
    Task<IReadOnlyList<Department>> GetByBranchAsync(Guid branchId, Guid operatorId);
    Task<Department>                CreateAsync(Department department);
    Task<Department?>               UpdateAsync(Department department);
    Task<bool>                      DeleteAsync(Guid id, Guid operatorId);
}
