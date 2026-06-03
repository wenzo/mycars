using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresDepartmentRepository : IDepartmentRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresDepartmentRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string Cols = "id, operator_id, branch_id, name, description, sort_order, is_active, created_at, updated_at";

    public async Task<IReadOnlyList<Department>> GetByOperatorAsync(Guid operatorId)
    {
        var sql = $"SELECT {Cols} FROM public.departments WHERE operator_id = @operatorId ORDER BY sort_order, name";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<Department>(sql, new { operatorId })).AsList();
    }

    public async Task<IReadOnlyList<Department>> GetByBranchAsync(Guid branchId, Guid operatorId)
    {
        var sql = $"SELECT {Cols} FROM public.departments WHERE branch_id = @branchId AND operator_id = @operatorId ORDER BY sort_order, name";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<Department>(sql, new { branchId, operatorId })).AsList();
    }

    public async Task<Department> CreateAsync(Department dept)
    {
        const string sql = """
            INSERT INTO public.departments (operator_id, branch_id, name, description, sort_order, is_active)
            VALUES (@OperatorId, @BranchId, @Name, @Description, @SortOrder, @IsActive)
            RETURNING id, operator_id, branch_id, name, description, sort_order, is_active, created_at, updated_at
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstAsync<Department>(sql, dept);
    }

    public async Task<Department?> UpdateAsync(Department dept)
    {
        const string sql = """
            UPDATE public.departments
            SET name = @Name, description = @Description, branch_id = @BranchId,
                sort_order = @SortOrder, is_active = @IsActive, updated_at = now()
            WHERE id = @Id AND operator_id = @OperatorId
            RETURNING id, operator_id, branch_id, name, description, sort_order, is_active, created_at, updated_at
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Department>(sql, dept);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        const string sql = "DELETE FROM public.departments WHERE id = @id AND operator_id = @operatorId";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, operatorId }) > 0;
    }
}
