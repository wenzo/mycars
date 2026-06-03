namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestOperatorRegistrationRepository : IOperatorRegistrationRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestOperatorRegistrationRepository(ISupabaseRestClient db) => _db = db;

    public async Task<OperatorRegistration> CreateAsync(OperatorRegistration reg)
    {
        reg.Id        = Guid.NewGuid();
        reg.Status    = "pending";
        reg.CreatedAt = DateTimeOffset.UtcNow;

        var result = await _db.InsertAsync<OperatorRegistration>("operator_registrations", reg);
        return result ?? reg;
    }

    public Task<IReadOnlyList<OperatorRegistration>> GetAllAsync(string? status = null)
    {
        var filter = status is not null ? $"status=eq.{status}" : null;
        return _db.SelectAsync<OperatorRegistration>(
            "operator_registrations", filter, order: "created_at.desc");
    }

    public Task<OperatorRegistration?> GetByIdAsync(Guid id)
        => _db.SelectOneAsync<OperatorRegistration>("operator_registrations", $"id=eq.{id}");

    public async Task<bool> UpdateStatusAsync(Guid id, string status, string? reviewNotes)
    {
        var result = await _db.UpdateAsync<OperatorRegistration>(
            "operator_registrations",
            $"id=eq.{id}&status=eq.pending",
            new { status, review_notes = reviewNotes, reviewed_at = DateTimeOffset.UtcNow });
        return result is not null;
    }
}
