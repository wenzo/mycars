namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestLeadRepository : ILeadRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestLeadRepository(ISupabaseRestClient db) => _db = db;

    public async Task<VehicleLead> CreateAsync(VehicleLead lead)
    {
        lead.Id        = Guid.NewGuid();
        lead.CreatedAt = DateTimeOffset.UtcNow;
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await _db.InsertAsync<VehicleLead>("vehicle_leads", lead);
        return result ?? throw new InvalidOperationException("Insert lead non ha restituito dati.");
    }

    public async Task<PagedResult<VehicleLead>> GetByOperatorAsync(
        Guid operatorId, string? status, PageRequest page)
    {
        var f = $"operator_id=eq.{operatorId}";
        if (!string.IsNullOrEmpty(status)) f += $"&status=eq.{status}";

        var total = await _db.CountAsync("vehicle_leads", f);
        var items = await _db.SelectAsync<VehicleLead>(
            "vehicle_leads", f, order: "created_at.desc",
            limit: page.PageSize, offset: page.Page * page.PageSize);
        return new PagedResult<VehicleLead>(items, total);
    }

    public Task<VehicleLead?> GetByIdAsync(Guid id, Guid operatorId)
        => _db.SelectOneAsync<VehicleLead>("vehicle_leads",
            $"id=eq.{id}&operator_id=eq.{operatorId}");

    public async Task<bool> UpdateStatusAsync(Guid id, Guid operatorId, string status)
    {
        var result = await _db.UpdateAsync<VehicleLead>(
            "vehicle_leads",
            $"id=eq.{id}&operator_id=eq.{operatorId}",
            new { status });
        return result is not null;
    }

    public Task<int> CountOpenAsync(Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&status=eq.new"
            : "status=eq.new";
        return _db.CountAsync("vehicle_leads", f).ContinueWith(t => (int)t.Result);
    }

    public Task<int> CountTestDrivePendingAsync(Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&lead_type=eq.test_drive&status=eq.new"
            : "lead_type=eq.test_drive&status=eq.new";
        return _db.CountAsync("vehicle_leads", f).ContinueWith(t => (int)t.Result);
    }

    public async Task<PagedResult<VehicleLead>> GetAllAsync(
        Guid operatorId, PageRequest page, string? status = null, string? leadType = null)
    {
        var parts = new List<string> { $"operator_id=eq.{operatorId}" };
        if (!string.IsNullOrEmpty(status))   parts.Add($"status=eq.{status}");
        if (!string.IsNullOrEmpty(leadType)) parts.Add($"lead_type=eq.{leadType}");

        var f     = string.Join("&", parts);
        var total = await _db.CountAsync("vehicle_leads", f);
        var items = await _db.SelectAsync<VehicleLead>(
            "vehicle_leads", f, order: "created_at.desc",
            limit: page.PageSize, offset: page.Page * page.PageSize);
        return new PagedResult<VehicleLead>(items, total);
    }

    public Task<IReadOnlyList<VehicleLead>> GetRecentAsync(int count, Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}"
            : null;
        return _db.SelectAsync<VehicleLead>(
            "vehicle_leads", f, order: "created_at.desc", limit: count);
    }
}
