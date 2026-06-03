namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestBranchRepository : IBranchRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestBranchRepository(ISupabaseRestClient db) => _db = db;

    private const string Cols = "id,operator_id,name,slug,legal_name,address,zip_code,city,province,country_code,latitude,longitude,phone,email,whatsapp_number,is_legal_address,is_active,sort_order,created_at,updated_at";

    public Task<IReadOnlyList<Branch>> GetByOperatorAsync(Guid operatorId)
        => _db.SelectAsync<Branch>("branches", $"operator_id=eq.{operatorId}",
            select: Cols, order: "sort_order.asc,name.asc");

    public Task<Branch?> GetByIdAsync(Guid id, Guid operatorId)
        => _db.SelectOneAsync<Branch>("branches", $"id=eq.{id}&operator_id=eq.{operatorId}", select: Cols);

    public async Task<Branch> CreateAsync(Branch branch)
    {
        var result = await _db.InsertAsync<Branch>("branches", new
        {
            operator_id      = branch.OperatorId,
            name             = branch.Name,
            slug             = branch.Slug,
            legal_name       = branch.LegalName,
            address          = branch.Address,
            zip_code         = branch.ZipCode,
            city             = branch.City,
            province         = branch.Province,
            country_code     = branch.CountryCode,
            latitude         = branch.Latitude,
            longitude        = branch.Longitude,
            phone            = branch.Phone,
            email            = branch.Email,
            whatsapp_number  = branch.WhatsappNumber,
            is_legal_address = branch.IsLegalAddress,
            is_active        = branch.IsActive,
            sort_order       = branch.SortOrder,
        });
        return result ?? throw new InvalidOperationException("Insert branch fallito.");
    }

    public Task<Branch?> UpdateAsync(Branch branch)
        => _db.UpdateAsync<Branch>("branches", $"id=eq.{branch.Id}&operator_id=eq.{branch.OperatorId}", new
        {
            name            = branch.Name,
            legal_name      = branch.LegalName,
            address         = branch.Address,
            zip_code        = branch.ZipCode,
            city            = branch.City,
            province        = branch.Province,
            phone           = branch.Phone,
            email           = branch.Email,
            whatsapp_number = branch.WhatsappNumber,
            latitude         = branch.Latitude,
            longitude        = branch.Longitude,
            is_legal_address = branch.IsLegalAddress,
            is_active        = branch.IsActive,
            sort_order       = branch.SortOrder,
            updated_at      = DateTimeOffset.UtcNow,
        });

    public Task DeleteAsync(Guid id, Guid operatorId)
        => _db.DeleteAsync("branches", $"id=eq.{id}&operator_id=eq.{operatorId}");

    Task<bool> IBranchRepository.DeleteAsync(Guid id, Guid operatorId)
    {
        _ = _db.DeleteAsync("branches", $"id=eq.{id}&operator_id=eq.{operatorId}");
        return Task.FromResult(true);
    }
}
