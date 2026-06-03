using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresOperatorUserRepository : IOperatorUserRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresOperatorUserRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<OperatorUser?> GetByEmailAsync(string email)
    {
        const string sql = """
            SELECT id, operator_id, email, password_hash, full_name, is_active, created_at, last_login_at
            FROM public.operator_users
            WHERE email = @email AND is_active = true
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OperatorUser>(sql, new { email });
    }

    public async Task<OperatorUser?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT id, operator_id, email, full_name, is_active, created_at, last_login_at
            FROM public.operator_users
            WHERE id = @id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OperatorUser>(sql, new { id });
    }

    public async Task UpdateLastLoginAsync(Guid id)
    {
        const string sql = "UPDATE public.operator_users SET last_login_at = now() WHERE id = @id";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new { id });
    }

    public async Task<OperatorUser> CreateAsync(OperatorUser user)
    {
        user.Id        = Guid.NewGuid();
        user.IsActive  = true;
        user.CreatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            INSERT INTO public.operator_users
                (id, operator_id, email, password_hash, full_name, is_active, created_at)
            VALUES
                (@Id, @OperatorId, @Email, @PasswordHash, @FullName, @IsActive, @CreatedAt)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, user);
        return user;
    }
}
