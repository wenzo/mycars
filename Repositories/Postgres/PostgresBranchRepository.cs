using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresBranchRepository : IBranchRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresBranchRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectCols = """
        id, operator_id, name, slug, legal_name, address, zip_code, city, province,
        country_code, latitude, longitude, phone, email, whatsapp_number, is_legal_address, is_active, sort_order, created_at, updated_at
        """;

    public async Task<IReadOnlyList<Branch>> GetByOperatorAsync(Guid operatorId)
    {
        var sql = $"SELECT {SelectCols} FROM public.branches WHERE operator_id = @operatorId ORDER BY sort_order, name";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<Branch>(sql, new { operatorId })).AsList();
    }

    public async Task<Branch?> GetByIdAsync(Guid id, Guid operatorId)
    {
        var sql = $"SELECT {SelectCols} FROM public.branches WHERE id = @id AND operator_id = @operatorId LIMIT 1";
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Branch>(sql, new { id, operatorId });
    }

    public async Task<Branch> CreateAsync(Branch branch)
    {
        const string sql = """
            INSERT INTO public.branches
                (operator_id, name, slug, legal_name, address, zip_code, city, province,
                 country_code, latitude, longitude, phone, email, whatsapp_number, is_legal_address, is_active, sort_order)
            VALUES
                (@OperatorId, @Name, @Slug, @LegalName, @Address, @ZipCode, @City, @Province,
                 @CountryCode, @Latitude, @Longitude, @Phone, @Email, @WhatsappNumber, @IsLegalAddress, @IsActive, @SortOrder)
            RETURNING id, operator_id, name, slug, city, province, latitude, longitude, is_legal_address, is_active, sort_order, created_at, updated_at
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstAsync<Branch>(sql, branch);
    }

    public async Task<Branch?> UpdateAsync(Branch branch)
    {
        const string sql = """
            UPDATE public.branches
            SET name = @Name, legal_name = @LegalName, address = @Address,
                zip_code = @ZipCode, city = @City, province = @Province,
                latitude = @Latitude, longitude = @Longitude,
                phone = @Phone, email = @Email, whatsapp_number = @WhatsappNumber,
                is_legal_address = @IsLegalAddress, is_active = @IsActive, sort_order = @SortOrder, updated_at = now()
            WHERE id = @Id AND operator_id = @OperatorId
            RETURNING id, operator_id, name, slug, city, province, latitude, longitude, is_legal_address, is_active, sort_order, created_at, updated_at
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Branch>(sql, branch);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        const string sql = "DELETE FROM public.branches WHERE id = @id AND operator_id = @operatorId";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, operatorId }) > 0;
    }
}
