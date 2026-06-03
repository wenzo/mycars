namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestOperatorRepository : IOperatorRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestOperatorRepository(ISupabaseRestClient db) => _db = db;

    public Task<OperatorProfile?> GetByIdAsync(Guid id)
        => _db.SelectOneAsync<OperatorProfile>("public_operator_profiles", $"id=eq.{id}");

    public Task<OperatorProfile?> GetBySlugAsync(string slug)
        => _db.SelectOneAsync<OperatorProfile>("public_operator_profiles", $"slug=eq.{slug}");

    public async Task<OperatorProfile?> GetByCodeAsync(string code)
    {
        // Step 1: risolve il codice in operator_id dalla tabella operator_app_codes
        var rows = await _db.SelectAsync<CodeRow>(
            "operator_app_codes",
            $"code=eq.{Uri.EscapeDataString(code)}&is_active=eq.true",
            select: "operator_id",
            limit: 1);
        if (rows.Count == 0) return null;

        // Step 2: recupera il profilo pubblico dell'operatore
        return await _db.SelectOneAsync<OperatorProfile>(
            "public_operator_profiles",
            $"id=eq.{rows[0].OperatorId}");
    }

    private sealed record CodeRow(Guid OperatorId);

    public Task<IReadOnlyList<OperatorProfile>> GetAllAsync()
        => _db.SelectAsync<OperatorProfile>("public_operator_profiles", order: "business_name.asc");

    public async Task<bool> SetActiveAsync(Guid id, bool isActive)
    {
        var result = await _db.UpdateAsync<OperatorProfile>("operators", $"id=eq.{id}",
            new { is_active = isActive, updated_at = DateTimeOffset.UtcNow });
        return result is not null;
    }

    public async Task<OperatorProfile?> UpdateAsync(OperatorProfile profile)
    {
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        return await _db.UpdateAsync<OperatorProfile>("operators", $"id=eq.{profile.Id}", new
        {
            business_name   = profile.BusinessName,
            vat_number      = profile.VatNumber,
            fiscal_code     = profile.FiscalCode,
            phone           = profile.Phone,
            email           = profile.Email,
            website_url     = profile.WebsiteUrl,
            whatsapp_number = profile.WhatsappNumber,
            primary_color   = profile.PrimaryColor,
            secondary_color = profile.SecondaryColor,
            accent_color    = profile.AccentColor,
            logo_url        = profile.LogoUrl,
            cover_image_url = profile.CoverImageUrl,
            updated_at      = profile.UpdatedAt,
        });
    }

    // ── App Codes ─────────────────────────────────────────────────────────────

    private const string AppCodeSelect =
        "id,operator_id,code,label,is_primary,is_active,expires_at,max_uses,use_count,created_at";

    public Task<IReadOnlyList<AppCode>> GetAppCodesAsync(Guid operatorId)
        => _db.SelectAsync<AppCode>("operator_app_codes",
            $"operator_id=eq.{operatorId}",
            select: AppCodeSelect,
            order: "is_primary.desc,created_at.asc");

    public async Task<AppCode> CreateAppCodeAsync(AppCode code)
    {
        code.Id        = Guid.NewGuid();
        code.CreatedAt = DateTimeOffset.UtcNow;
        var result = await _db.InsertAsync<AppCode>("operator_app_codes", new
        {
            id          = code.Id,
            operator_id = code.OperatorId,
            code        = code.Code,
            label       = code.Label,
            is_primary  = code.IsPrimary,
            is_active   = code.IsActive,
            expires_at  = code.ExpiresAt,
            max_uses    = code.MaxUses,
            use_count   = 0,
            created_at  = code.CreatedAt,
        });
        return result ?? code;
    }

    public async Task<bool> DeleteAppCodeAsync(Guid id, Guid operatorId)
    {
        var existing = await _db.SelectOneAsync<AppCode>("operator_app_codes",
            $"id=eq.{id}&operator_id=eq.{operatorId}&is_primary=eq.false",
            select: "id");
        if (existing is null) return false;

        await _db.UpdateAsync<AppCode>("operator_app_codes", $"id=eq.{id}",
            new { is_active = false, updated_at = DateTimeOffset.UtcNow });
        return true;
    }

    public async Task<OperatorProfile?> ResolveCodeAsync(string code)
    {
        var row = await _db.SelectOneAsync<AppCode>("operator_app_codes",
            $"code=eq.{Uri.EscapeDataString(code)}&is_active=eq.true",
            select: AppCodeSelect);

        if (row is null) return null;
        if (row.ExpiresAt.HasValue && row.ExpiresAt.Value <= DateTimeOffset.UtcNow) return null;
        if (row.MaxUses.HasValue && row.UseCount >= row.MaxUses.Value) return null;

        await _db.UpdateAsync<AppCode>("operator_app_codes", $"id=eq.{row.Id}",
            new { use_count = row.UseCount + 1, updated_at = DateTimeOffset.UtcNow });

        return await _db.SelectOneAsync<OperatorProfile>(
            "public_operator_profiles", $"id=eq.{row.OperatorId}");
    }

    public async Task<OperatorProfile> CreateAsync(OperatorProfile profile)
    {
        profile.Id        = Guid.NewGuid();
        profile.IsActive  = true;
        profile.CreatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _db.InsertAsync<OperatorProfile>("operators", new
        {
            id            = profile.Id,
            business_name = profile.BusinessName,
            slug          = profile.Slug,
            public_code   = profile.PublicCode,
            website_url   = profile.WebsiteUrl,
            phone         = profile.Phone,
            email         = profile.Email,
            is_active     = profile.IsActive,
            created_at    = profile.CreatedAt,
            updated_at    = profile.UpdatedAt,
        });
        return result ?? profile;
    }
}
