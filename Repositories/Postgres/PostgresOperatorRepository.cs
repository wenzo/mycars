using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresOperatorRepository : IOperatorRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresOperatorRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectCols = """
        o.id, o.business_name, o.slug, o.public_code,
        o.vat_number, o.fiscal_code, o.rea_number,
        o.website_url, o.phone, o.email, o.whatsapp_number,
        o.address, o.city, o.province, o.zip_code,
        o.latitude, o.longitude,
        o.primary_color, o.secondary_color, o.accent_color,
        o.tagline, o.is_active, o.created_at, o.updated_at,
        o.cover_image_url,
        COALESCE(o.logo_url, ma.public_url) AS logo_url,
        o.rental_module_enabled, o.rental_photos_enabled,
        o.rental_contract_pdf_enabled, o.rental_show_prices,
        o.smtp_host, o.smtp_port, o.smtp_use_ssl,
        o.smtp_username, o.smtp_password, o.smtp_from_email, o.smtp_from_name
        """;

    public async Task<OperatorProfile?> GetByIdAsync(Guid id)
    {
        var sql = $"""
            SELECT {SelectCols}
            FROM public.operators o
            LEFT JOIN public.media_assets ma ON ma.id = o.logo_media_id AND ma.operator_id = o.id
            WHERE o.id = @id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OperatorProfile>(sql, new { id });
    }

    public async Task<OperatorProfile?> GetBySlugAsync(string slug)
    {
        var sql = $"""
            SELECT {SelectCols}
            FROM public.operators o
            LEFT JOIN public.media_assets ma ON ma.id = o.logo_media_id AND ma.operator_id = o.id
            WHERE o.slug = @slug AND o.is_active = true
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OperatorProfile>(sql, new { slug });
    }

    public async Task<OperatorProfile?> GetByCodeAsync(string code)
    {
        var sql = $"""
            SELECT {SelectCols}
            FROM public.operator_app_codes c
            JOIN public.operators o ON o.id = c.operator_id
            LEFT JOIN public.media_assets ma ON ma.id = o.logo_media_id AND ma.operator_id = o.id
            WHERE c.code = @code
              AND c.is_active = true AND o.is_active = true
              AND (c.expires_at IS NULL OR c.expires_at > now())
              AND (c.max_uses IS NULL OR c.use_count < c.max_uses)
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OperatorProfile>(sql, new { code });
    }

    public async Task<IReadOnlyList<OperatorProfile>> GetAllAsync()
    {
        var sql = $"""
            SELECT {SelectCols}
            FROM public.operators o
            LEFT JOIN public.media_assets ma ON ma.id = o.logo_media_id AND ma.operator_id = o.id
            ORDER BY o.business_name
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<OperatorProfile>(sql)).AsList();
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive)
    {
        const string sql = "UPDATE public.operators SET is_active = @isActive, updated_at = now() WHERE id = @id";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, isActive }) > 0;
    }

    public async Task<OperatorProfile?> UpdateAsync(OperatorProfile profile)
    {
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        const string sql = """
            UPDATE public.operators SET
                business_name               = @BusinessName,
                vat_number                  = @VatNumber,
                fiscal_code                 = @FiscalCode,
                rea_number                  = @ReaNumber,
                phone                       = @Phone,
                email                       = @Email,
                website_url                 = @WebsiteUrl,
                whatsapp_number             = @WhatsappNumber,
                address                     = @Address,
                city                        = @City,
                province                    = @Province,
                zip_code                    = @ZipCode,
                latitude                    = @Latitude,
                longitude                   = @Longitude,
                primary_color               = @PrimaryColor,
                secondary_color             = @SecondaryColor,
                accent_color                = @AccentColor,
                tagline                     = @Tagline,
                logo_url                    = @LogoUrl,
                cover_image_url             = @CoverImageUrl,
                rental_module_enabled       = @RentalModuleEnabled,
                rental_photos_enabled       = @RentalPhotosEnabled,
                rental_contract_pdf_enabled = @RentalContractPdfEnabled,
                rental_show_prices          = @RentalShowPrices,
                smtp_host                   = @SmtpHost,
                smtp_port                   = @SmtpPort,
                smtp_use_ssl                = @SmtpUseSsl,
                smtp_username               = @SmtpUsername,
                smtp_password               = @SmtpPassword,
                smtp_from_email             = @SmtpFromEmail,
                smtp_from_name              = @SmtpFromName,
                updated_at                  = @UpdatedAt
            WHERE id = @Id
            """;
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, profile);
        return rows > 0 ? profile : null;
    }

    // ── App Codes ─────────────────────────────────────────────────────────────

    private const string AppCodeCols =
        "id, operator_id, code, label, is_primary, is_active, expires_at, max_uses, use_count, created_at";

    public async Task<IReadOnlyList<AppCode>> GetAppCodesAsync(Guid operatorId)
    {
        var sql = $"""
            SELECT {AppCodeCols} FROM public.operator_app_codes
            WHERE operator_id = @operatorId
            ORDER BY is_primary DESC, created_at ASC
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<AppCode>(sql, new { operatorId })).AsList();
    }

    public async Task<AppCode> CreateAppCodeAsync(AppCode code)
    {
        code.Id        = Guid.NewGuid();
        code.CreatedAt = DateTimeOffset.UtcNow;

        var sql = $"""
            INSERT INTO public.operator_app_codes
                (id, operator_id, code, label, is_primary, is_active, expires_at, max_uses, use_count, created_at)
            VALUES
                (@Id, @OperatorId, @Code, @Label, @IsPrimary, @IsActive, @ExpiresAt, @MaxUses, 0, @CreatedAt)
            RETURNING {AppCodeCols}
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstAsync<AppCode>(sql, code);
    }

    public async Task<AppCode?> UpdateAppCodeAsync(Guid id, Guid operatorId, string newCode)
    {
        var sql = $"""
            UPDATE public.operator_app_codes
            SET code = @newCode, updated_at = now()
            WHERE id = @id AND operator_id = @operatorId AND is_active = true
            RETURNING {AppCodeCols}
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AppCode>(sql, new { id, operatorId, newCode });
    }

    public async Task<bool> DeleteAppCodeAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            UPDATE public.operator_app_codes
            SET is_active = false, updated_at = now()
            WHERE id = @id AND operator_id = @operatorId AND is_primary = false
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, operatorId }) > 0;
    }

    public async Task<OperatorProfile?> ResolveCodeAsync(string code)
    {
        const string findSql = """
            SELECT id, operator_id, use_count, max_uses, expires_at
            FROM public.operator_app_codes
            WHERE code = @code AND is_active = true
              AND (expires_at IS NULL OR expires_at > now())
              AND (max_uses IS NULL OR use_count < max_uses)
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<AppCode>(findSql, new { code });
        if (row is null) return null;

        await conn.ExecuteAsync(
            "UPDATE public.operator_app_codes SET use_count = use_count + 1, updated_at = now() WHERE id = @id",
            new { id = row.Id });

        var sql = $"""
            SELECT {SelectCols}
            FROM public.operators o
            LEFT JOIN public.media_assets ma ON ma.id = o.logo_media_id AND ma.operator_id = o.id
            WHERE o.id = @id AND o.is_active = true
            LIMIT 1
            """;
        return await conn.QueryFirstOrDefaultAsync<OperatorProfile>(sql, new { id = row.OperatorId });
    }

    public async Task<OperatorProfile> CreateAsync(OperatorProfile profile)
    {
        profile.Id        = Guid.NewGuid();
        profile.IsActive  = true;
        profile.CreatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            INSERT INTO public.operators
                (id, business_name, slug, public_code,
                 website_url, phone, email,
                 is_active, created_at, updated_at)
            VALUES
                (@Id, @BusinessName, @Slug, @PublicCode,
                 @WebsiteUrl, @Phone, @Email,
                 @IsActive, @CreatedAt, @UpdatedAt)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, profile);
        return profile;
    }
}
