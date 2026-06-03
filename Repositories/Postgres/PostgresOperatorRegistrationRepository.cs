using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresOperatorRegistrationRepository : IOperatorRegistrationRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresOperatorRegistrationRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<OperatorRegistration> CreateAsync(OperatorRegistration reg)
    {
        reg.Id        = Guid.NewGuid();
        reg.Status    = "pending";
        reg.CreatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            INSERT INTO public.operator_registrations
                (id, business_name, vat_number, email, phone, contact_person,
                 address, city, province, website, notes, status, created_at)
            VALUES
                (@Id, @BusinessName, @VatNumber, @Email, @Phone, @ContactPerson,
                 @Address, @City, @Province, @Website, @Notes, @Status, @CreatedAt)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, reg);
        return reg;
    }

    public async Task<IReadOnlyList<OperatorRegistration>> GetAllAsync(string? status = null)
    {
        var sql = status is not null
            ? """
              SELECT id, business_name, vat_number, email, phone, contact_person,
                     address, city, province, website, notes,
                     status, reviewed_at, review_notes, created_at
              FROM public.operator_registrations
              WHERE status = @status
              ORDER BY created_at DESC
              """
            : """
              SELECT id, business_name, vat_number, email, phone, contact_person,
                     address, city, province, website, notes,
                     status, reviewed_at, review_notes, created_at
              FROM public.operator_registrations
              ORDER BY created_at DESC
              """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<OperatorRegistration>(sql,
            status is not null ? new { status } : null)).AsList();
    }

    public async Task<OperatorRegistration?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT id, business_name, vat_number, email, phone, contact_person,
                   address, city, province, website, notes,
                   status, reviewed_at, review_notes, created_at
            FROM public.operator_registrations
            WHERE id = @id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OperatorRegistration>(sql, new { id });
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, string? reviewNotes)
    {
        const string sql = """
            UPDATE public.operator_registrations
            SET status       = @status,
                reviewed_at  = now(),
                review_notes = @reviewNotes
            WHERE id = @id AND status = 'pending'
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, status, reviewNotes }) > 0;
    }
}
