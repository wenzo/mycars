namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestOperatorUserRepository : IOperatorUserRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestOperatorUserRepository(ISupabaseRestClient db) => _db = db;

    public Task<OperatorUser?> GetByEmailAsync(string email)
        => _db.SelectOneAsync<OperatorUser>("operator_users",
            $"email=eq.{Uri.EscapeDataString(email)}&is_active=eq.true",
            select: "id,operator_id,email,password_hash,full_name,is_active,created_at,last_login_at");

    public Task<OperatorUser?> GetByIdAsync(Guid id)
        => _db.SelectOneAsync<OperatorUser>("operator_users",
            $"id=eq.{id}",
            select: "id,operator_id,email,full_name,is_active,created_at,last_login_at");

    public Task UpdateLastLoginAsync(Guid id)
        => _db.UpdateAsync<OperatorUser>("operator_users", $"id=eq.{id}",
            new { last_login_at = DateTimeOffset.UtcNow });

    public async Task<OperatorUser> CreateAsync(OperatorUser user)
    {
        user.Id        = Guid.NewGuid();
        user.IsActive  = true;
        user.CreatedAt = DateTimeOffset.UtcNow;

        var result = await _db.InsertAsync<OperatorUser>("operator_users", new
        {
            id            = user.Id,
            operator_id   = user.OperatorId,
            email         = user.Email,
            password_hash = user.PasswordHash,
            full_name     = user.FullName,
            is_active     = user.IsActive,
            created_at    = user.CreatedAt,
        });
        return result ?? user;
    }
}
