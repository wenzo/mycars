namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestDepartmentRepository : IDepartmentRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestDepartmentRepository(ISupabaseRestClient db) => _db = db;

    private const string Cols = "id,operator_id,branch_id,name,description,responsible_name,phone,email,sort_order,is_active,created_at,updated_at";

    public Task<IReadOnlyList<Department>> GetByOperatorAsync(Guid operatorId)
        => _db.SelectAsync<Department>("departments", $"operator_id=eq.{operatorId}",
            select: Cols, order: "sort_order.asc,name.asc");

    public Task<IReadOnlyList<Department>> GetByBranchAsync(Guid branchId, Guid operatorId)
        => _db.SelectAsync<Department>("departments",
            $"branch_id=eq.{branchId}&operator_id=eq.{operatorId}", select: Cols, order: "sort_order.asc,name.asc");

    public async Task<Department> CreateAsync(Department dept)
    {
        var result = await _db.InsertAsync<Department>("departments", new
        {
            operator_id      = dept.OperatorId,
            branch_id        = dept.BranchId,
            name             = dept.Name,
            description      = dept.Description,
            responsible_name = dept.ResponsibleName,
            phone            = dept.Phone,
            email            = dept.Email,
            sort_order       = dept.SortOrder,
            is_active        = dept.IsActive,
        });
        return result ?? throw new InvalidOperationException("Insert department fallito.");
    }

    public Task<Department?> UpdateAsync(Department dept)
        => _db.UpdateAsync<Department>("departments",
            $"id=eq.{dept.Id}&operator_id=eq.{dept.OperatorId}", new
            {
                name             = dept.Name,
                description      = dept.Description,
                responsible_name = dept.ResponsibleName,
                phone            = dept.Phone,
                email            = dept.Email,
                branch_id        = dept.BranchId,
                sort_order       = dept.SortOrder,
                is_active        = dept.IsActive,
                updated_at       = DateTimeOffset.UtcNow,
            });

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        await _db.DeleteAsync("departments", $"id=eq.{id}&operator_id=eq.{operatorId}");
        return true;
    }
}
