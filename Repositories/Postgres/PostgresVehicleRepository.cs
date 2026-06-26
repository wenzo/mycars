using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresVehicleRepository : IVehicleRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresVehicleRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<VehicleCard>> GetPublicCardsAsync(
        Guid operatorId, PageRequest page, VehicleFilter? f = null)
    {
        var (where, param) = BuildWherePublic(operatorId, f);
        var orderBy = BuildOrderBy(f);

        var countSql = $"SELECT COUNT(*) FROM public.public_vehicle_cards WHERE {where}";
        var itemsSql = $"""
            SELECT id, operator_id, operator_slug, operator_code,
                   branch_id, internal_code,
                   vehicle_type::text AS vehicle_type,
                   brand_name, brand_slug, model, version, body_type_name,
                   condition::text AS condition,
                   usage_type::text AS usage_type,
                   fuel::text AS fuel,
                   transmission::text AS transmission,
                   horsepower_cv, power_kw, engine_capacity_cc,
                   registration_month, registration_year, mileage_km, doors, seats,
                   color, emission_class, damaged,
                   price, previous_price, currency,
                   is_sold, show_as_sold, pronta_consegna, is_nuovo_arrivo, nuovo_arrivo_until,
                   description, cover_image_url, cover_bucket, cover_storage_path,
                   branch_name, city, province, created_at, updated_at
            FROM public.public_vehicle_cards
            WHERE {where}
            ORDER BY {orderBy}
            LIMIT @pageSize OFFSET @offset
            """;
        param.Add("pageSize", page.PageSize);
        param.Add("offset",   page.Page * page.PageSize);

        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, param);
        var items = (await conn.QueryAsync<VehicleCard>(itemsSql, param)).AsList();
        return new PagedResult<VehicleCard>(items, total);
    }

    public async Task<VehicleCard?> GetCardByIdAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            SELECT id, operator_id, operator_slug, operator_code,
                   branch_id, internal_code,
                   vehicle_type::text AS vehicle_type,
                   brand_name, brand_slug, model, version, body_type_name,
                   condition::text AS condition,
                   usage_type::text AS usage_type,
                   fuel::text AS fuel,
                   transmission::text AS transmission,
                   horsepower_cv, power_kw,
                   registration_month, registration_year, mileage_km,
                   price, previous_price, currency,
                   is_sold, show_as_sold, pronta_consegna, is_nuovo_arrivo, nuovo_arrivo_until,
                   description, cover_image_url, cover_bucket, cover_storage_path,
                   branch_name, city, province, created_at, updated_at
            FROM public.public_vehicle_cards
            WHERE id = @id AND operator_id = @operatorId
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<VehicleCard>(sql, new { id, operatorId });
    }

    public async Task<PagedResult<Vehicle>> GetByOperatorAsync(Guid operatorId, PageRequest page)
    {
        const string countSql =
            "SELECT COUNT(*) FROM public.vehicles WHERE operator_id = @operatorId AND deleted_at IS NULL";
        const string itemsSql = """
            SELECT id, operator_id, branch_id, department_id,
                   internal_code, external_code, vin,
                   vehicle_type::text AS vehicle_type,
                   brand_id, model, version, body_type_id,
                   usage_type::text AS usage_type,
                   condition::text AS condition,
                   fuel::text AS fuel,
                   transmission::text AS transmission,
                   engine_capacity_cc, horsepower_cv, power_kw,
                   registration_month, registration_year, mileage_km,
                   doors, seats, color, emission_class,
                   handicap_accessible, vat_deductible, damaged, imported,
                   description, equipment::text AS equipment,
                   price, previous_price, currency, negotiable, listing_date,
                   is_sold, show_as_sold, sold_at,
                   pronta_consegna, is_nuovo_arrivo, nuovo_arrivo_until,
                   is_published, published_at, sort_order,
                   created_at, updated_at, deleted_at
            FROM public.vehicles
            WHERE operator_id = @operatorId AND deleted_at IS NULL
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset
            """;
        var param = new { operatorId, pageSize = page.PageSize, offset = page.Page * page.PageSize };
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, param);
        var items = (await conn.QueryAsync<Vehicle>(itemsSql, param)).AsList();
        return new PagedResult<Vehicle>(items, total);
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            SELECT v.id, v.operator_id, v.branch_id, v.department_id,
                   v.internal_code, v.vin, v.targa,
                   v.vehicle_type::text AS vehicle_type,
                   v.brand_id, b.name AS brand_name,
                   v.model, v.version,
                   v.condition::text AS condition,
                   v.fuel::text AS fuel,
                   v.transmission::text AS transmission,
                   v.horsepower_cv, v.power_kw, v.registration_year, v.mileage_km,
                   v.color, v.price, v.previous_price,
                   v.negotiable, v.is_published, v.pronta_consegna, v.is_nuovo_arrivo,
                   v.description, v.sort_order, v.created_at, v.updated_at
            FROM public.vehicles v
            LEFT JOIN public.brands b ON b.id = v.brand_id
            WHERE v.id = @id AND v.operator_id = @operatorId AND v.deleted_at IS NULL
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Vehicle>(sql, new { id, operatorId });
    }

    public async Task<IReadOnlyList<BrandInfo>> GetBrandsAsync()
    {
        const string sql = "SELECT id, name, slug FROM public.brands ORDER BY name";
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<BrandInfo>(sql)).AsList();
    }

    public async Task<IReadOnlyList<BrandInfo>> GetBrandsWithTypesAsync()
    {
        const string brandsSql = "SELECT id, name, slug FROM public.brands ORDER BY name";
        const string typesSql  = "SELECT brand_id, vehicle_type::text AS vehicle_type FROM public.brand_vehicle_types";
        using var conn = _factory.CreateConnection();
        var brands = (await conn.QueryAsync<BrandInfo>(brandsSql)).AsList();
        var types  = (await conn.QueryAsync<(Guid BrandId, string VehicleType)>(typesSql)).AsList();
        var map    = types.GroupBy(x => x.BrandId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.VehicleType).ToArray());
        foreach (var b in brands)
            b.VehicleTypes = map.TryGetValue(b.Id, out var t) ? t : [];
        return brands;
    }

    public async Task<BrandInfo> CreateBrandAsync(string name, string[] vehicleTypes)
    {
        var slug = Slugify(name);
        const string sql = """
            INSERT INTO public.brands (name, slug)
            VALUES (@name, @slug)
            ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name
            RETURNING id, name, slug
            """;
        using var conn  = _factory.CreateConnection();
        var brand       = await conn.QueryFirstAsync<BrandInfo>(sql, new { name = name.Trim(), slug });
        foreach (var vt in vehicleTypes)
            await conn.ExecuteAsync("""
                INSERT INTO public.brand_vehicle_types (brand_id, vehicle_type)
                VALUES (@id, @vt::public.vehicle_type)
                ON CONFLICT DO NOTHING
                """, new { id = brand.Id, vt });
        brand.VehicleTypes = vehicleTypes;
        return brand;
    }

    public async Task<BrandInfo?> UpdateBrandAsync(Guid id, string name, string[] vehicleTypes)
    {
        var slug  = Slugify(name);
        const string sql = """
            UPDATE public.brands SET name = @name, slug = @slug
            WHERE id = @id
            RETURNING id, name, slug
            """;
        using var conn = _factory.CreateConnection();
        var brand      = await conn.QueryFirstOrDefaultAsync<BrandInfo>(sql, new { id, name = name.Trim(), slug });
        if (brand is null) return null;
        await conn.ExecuteAsync("DELETE FROM public.brand_vehicle_types WHERE brand_id = @id", new { id });
        foreach (var vt in vehicleTypes)
            await conn.ExecuteAsync("""
                INSERT INTO public.brand_vehicle_types (brand_id, vehicle_type)
                VALUES (@id, @vt::public.vehicle_type)
                ON CONFLICT DO NOTHING
                """, new { id, vt });
        brand.VehicleTypes = vehicleTypes;
        return brand;
    }

    public async Task<bool> DeleteBrandAsync(Guid id)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM public.brand_vehicle_types WHERE brand_id = @id", new { id });
        return await conn.ExecuteAsync("DELETE FROM public.brands WHERE id = @id", new { id }) > 0;
    }

    private static string Slugify(string name)
    {
        var s = name.ToLowerInvariant().Trim()
            .Replace("ä", "a").Replace("á", "a").Replace("à", "a").Replace("â", "a")
            .Replace("ë", "e").Replace("é", "e").Replace("è", "e").Replace("ê", "e")
            .Replace("ï", "i").Replace("í", "i").Replace("ì", "i").Replace("î", "i")
            .Replace("ö", "o").Replace("ó", "o").Replace("ò", "o").Replace("ô", "o")
            .Replace("ü", "u").Replace("ú", "u").Replace("ù", "u").Replace("û", "u")
            .Replace("ñ", "n").Replace("ç", "c").Replace("ß", "ss");
        return System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
    }

    public async Task<Vehicle> CreateAsync(Vehicle vehicle, string brandName)
    {
        using var conn = _factory.CreateConnection();
        vehicle.Id        = Guid.NewGuid();
        vehicle.BrandId   = await ResolveBrandAsync(conn, brandName, vehicle.VehicleType);
        vehicle.CreatedAt = DateTimeOffset.UtcNow;
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            INSERT INTO public.vehicles
                (id, operator_id, branch_id, internal_code, targa,
                 vehicle_type, brand_id, model, version,
                 condition, fuel, transmission,
                 horsepower_cv, power_kw, registration_year, mileage_km, color,
                 price, previous_price, negotiable,
                 is_published, published_at, pronta_consegna, is_nuovo_arrivo, description,
                 created_at, updated_at)
            VALUES
                (@Id, @OperatorId, @BranchId, @InternalCode, @Targa,
                 @VehicleType::public.vehicle_type, @BrandId, @Model, @Version,
                 @Condition::public.vehicle_condition, @Fuel::public.fuel_type,
                 @Transmission::public.transmission_type,
                 @HorsepowerCv, @PowerKw, @RegistrationYear, @MileageKm, @Color,
                 @Price, @PreviousPrice, @Negotiable,
                 @IsPublished, @PublishedAt, @ProntaConsegna, @IsNuovoArrivo, @Description,
                 @CreatedAt, @UpdatedAt)
            RETURNING id, operator_id, branch_id, internal_code,
                      vehicle_type::text AS vehicle_type, brand_id, model, version,
                      condition::text AS condition, is_published,
                      pronta_consegna, is_nuovo_arrivo, price, created_at, updated_at
            """;
        return await conn.QueryFirstAsync<Vehicle>(sql, vehicle);
    }

    public async Task<Vehicle?> UpdateAsync(Vehicle vehicle, string brandName)
    {
        using var conn = _factory.CreateConnection();
        vehicle.BrandId   = await ResolveBrandAsync(conn, brandName, vehicle.VehicleType);
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        const string sql = """
            UPDATE public.vehicles SET
                branch_id        = @BranchId,
                internal_code    = @InternalCode,
                targa            = @Targa,
                vehicle_type     = @VehicleType::public.vehicle_type,
                brand_id         = @BrandId,
                model            = @Model,
                version          = @Version,
                condition        = @Condition::public.vehicle_condition,
                fuel             = @Fuel::public.fuel_type,
                transmission     = @Transmission::public.transmission_type,
                horsepower_cv    = @HorsepowerCv,
                power_kw         = @PowerKw,
                registration_year = @RegistrationYear,
                mileage_km       = @MileageKm,
                color            = @Color,
                price            = @Price,
                previous_price   = @PreviousPrice,
                negotiable       = @Negotiable,
                is_published     = @IsPublished,
                published_at     = CASE WHEN @IsPublished AND published_at IS NULL THEN now() ELSE published_at END,
                pronta_consegna  = @ProntaConsegna,
                is_nuovo_arrivo  = @IsNuovoArrivo,
                description      = @Description,
                updated_at       = @UpdatedAt
            WHERE id = @Id AND operator_id = @OperatorId AND deleted_at IS NULL
            RETURNING id, operator_id, branch_id, internal_code,
                      vehicle_type::text AS vehicle_type, brand_id, model, version,
                      condition::text AS condition, is_published,
                      pronta_consegna, is_nuovo_arrivo, price, created_at, updated_at
            """;
        return await conn.QueryFirstOrDefaultAsync<Vehicle>(sql, vehicle);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            UPDATE public.vehicles
            SET deleted_at = now(), updated_at = now()
            WHERE id = @id AND operator_id = @operatorId AND deleted_at IS NULL
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, operatorId }) > 0;
    }

    private static async Task<Guid> ResolveBrandAsync(
        System.Data.IDbConnection conn, string brandName, string vehicleType)
    {
        var slug = System.Text.RegularExpressions.Regex.Replace(
            brandName.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

        var brandId = await conn.ExecuteScalarAsync<Guid>("""
            INSERT INTO public.brands (name, slug)
            VALUES (@name, @slug)
            ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name
            RETURNING id
            """, new { name = brandName.Trim(), slug });

        await conn.ExecuteAsync("""
            INSERT INTO public.brand_vehicle_types (brand_id, vehicle_type)
            VALUES (@brandId, @vehicleType::public.vehicle_type)
            ON CONFLICT DO NOTHING
            """, new { brandId, vehicleType });

        return brandId;
    }

    public async Task<int> CountActiveAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? "SELECT COUNT(*) FROM public.vehicles WHERE operator_id = @id AND is_published = true AND deleted_at IS NULL"
            : "SELECT COUNT(*) FROM public.vehicles WHERE is_published = true AND deleted_at IS NULL";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, operatorId.HasValue ? new { id = operatorId } : null);
    }

    public async Task<int> CountNuoviArriviAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? "SELECT COUNT(*) FROM public.vehicles WHERE operator_id = @id AND is_nuovo_arrivo = true AND deleted_at IS NULL"
            : "SELECT COUNT(*) FROM public.vehicles WHERE is_nuovo_arrivo = true AND deleted_at IS NULL";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, operatorId.HasValue ? new { id = operatorId } : null);
    }

    public async Task<int> CountProntaConsegnaAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? "SELECT COUNT(*) FROM public.vehicles WHERE operator_id = @id AND pronta_consegna = true AND deleted_at IS NULL"
            : "SELECT COUNT(*) FROM public.vehicles WHERE pronta_consegna = true AND deleted_at IS NULL";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, operatorId.HasValue ? new { id = operatorId } : null);
    }

    public async Task<PagedResult<Vehicle>> GetAllAsync(
        Guid operatorId, PageRequest page, string? condition = null, bool? isPublished = null,
        bool? isNuovoArrivo = null, bool? prontaConsegna = null,
        bool? vatDeductible = null, bool? handicapAccessible = null,
        bool? imported = null, bool? forSale = null, bool? forRental = null)
    {
        var parts = new List<string> { "operator_id = @operatorId", "deleted_at IS NULL" };
        var p     = new DynamicParameters();
        p.Add("operatorId", operatorId);

        if (!string.IsNullOrEmpty(condition))
            { parts.Add("condition = @condition::public.vehicle_condition"); p.Add("condition", condition); }
        if (isPublished.HasValue)
            { parts.Add("is_published = @isPublished"); p.Add("isPublished", isPublished.Value); }
        if (isNuovoArrivo.HasValue)
            { parts.Add("is_nuovo_arrivo = @isNuovoArrivo"); p.Add("isNuovoArrivo", isNuovoArrivo.Value); }
        if (prontaConsegna.HasValue)
            { parts.Add("pronta_consegna = @prontaConsegna"); p.Add("prontaConsegna", prontaConsegna.Value); }
        if (vatDeductible.HasValue)
            { parts.Add("vat_deductible = @vatDeductible"); p.Add("vatDeductible", vatDeductible.Value); }
        if (handicapAccessible.HasValue)
            { parts.Add("handicap_accessible = @handicapAccessible"); p.Add("handicapAccessible", handicapAccessible.Value); }
        if (imported.HasValue)
            { parts.Add("imported = @imported"); p.Add("imported", imported.Value); }
        if (forSale.HasValue)
            { parts.Add("for_sale = @forSale"); p.Add("forSale", forSale.Value); }
        if (forRental.HasValue)
            { parts.Add("for_rental = @forRental"); p.Add("forRental", forRental.Value); }

        p.Add("pageSize", page.PageSize);
        p.Add("offset",   page.Page * page.PageSize);

        var where    = string.Join(" AND ", parts);
        var countSql = $"SELECT COUNT(*) FROM public.vehicles WHERE {where}";
        var itemsSql = $"""
            SELECT id, operator_id, brand_id, model, version,
                   vehicle_type::text AS vehicle_type,
                   condition::text AS condition,
                   internal_code, is_published,
                   is_nuovo_arrivo, pronta_consegna,
                   price, created_at, updated_at
            FROM public.vehicles
            WHERE {where}
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset
            """;
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, p);
        var items = (await conn.QueryAsync<Vehicle>(itemsSql, p)).AsList();
        return new PagedResult<Vehicle>(items, total);
    }

    public async Task<Vehicle?> FindByTargaAsync(string targa, Guid operatorId)
    {
        const string sql = """
            SELECT id, operator_id, model, version, internal_code, targa
            FROM public.vehicles
            WHERE targa = @targa AND operator_id = @operatorId AND deleted_at IS NULL
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Vehicle>(sql, new { targa, operatorId });
    }

    public async Task<IReadOnlyList<Vehicle>> GetRecentAsync(int count, Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? """
              SELECT id, operator_id, brand_id, model, version,
                     vehicle_type::text AS vehicle_type,
                     condition::text AS condition,
                     price, is_nuovo_arrivo, pronta_consegna,
                     internal_code, is_published, created_at, updated_at
              FROM public.vehicles
              WHERE operator_id = @id AND deleted_at IS NULL
              ORDER BY created_at DESC LIMIT @count
              """
            : """
              SELECT id, operator_id, brand_id, model, version,
                     vehicle_type::text AS vehicle_type,
                     condition::text AS condition,
                     price, is_nuovo_arrivo, pronta_consegna,
                     internal_code, is_published, created_at, updated_at
              FROM public.vehicles
              WHERE deleted_at IS NULL
              ORDER BY created_at DESC LIMIT @count
              """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<Vehicle>(sql,
            operatorId.HasValue ? new { id = operatorId, count } : (object)new { count })).AsList();
    }

    // ── Images ────────────────────────────────────────────────────────────────

    public async Task UpdateCoverAsync(Guid vehicleId, Guid operatorId, string? coverUrl)
    {
        const string sql = """
            UPDATE public.vehicles
            SET cover_image_url = @coverUrl, updated_at = now()
            WHERE id = @vehicleId AND operator_id = @operatorId AND deleted_at IS NULL
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new { coverUrl, vehicleId, operatorId });
    }

    public async Task<IReadOnlyList<VehicleImage>> GetImagesAsync(Guid vehicleId, Guid operatorId)
    {
        const string sql = """
            SELECT id, vehicle_id, operator_id, url, sort_order, created_at
            FROM public.vehicle_images
            WHERE vehicle_id = @vehicleId AND operator_id = @operatorId
            ORDER BY sort_order, created_at
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<VehicleImage>(sql, new { vehicleId, operatorId })).AsList();
    }

    public async Task<VehicleImage> AddImageAsync(VehicleImage image)
    {
        image.Id        = Guid.NewGuid();
        image.CreatedAt = DateTimeOffset.UtcNow;
        const string sql = """
            INSERT INTO public.vehicle_images (id, vehicle_id, operator_id, url, sort_order, created_at)
            VALUES (@Id, @VehicleId, @OperatorId, @Url, @SortOrder, @CreatedAt)
            RETURNING id, vehicle_id, operator_id, url, sort_order, created_at
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstAsync<VehicleImage>(sql, image);
    }

    public async Task<bool> DeleteImageAsync(Guid imageId, Guid vehicleId, Guid operatorId)
    {
        const string sql = """
            DELETE FROM public.vehicle_images
            WHERE id = @imageId AND vehicle_id = @vehicleId AND operator_id = @operatorId
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { imageId, vehicleId, operatorId }) > 0;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static (string Where, Dapper.DynamicParameters Param) BuildWherePublic(
        Guid operatorId, VehicleFilter? f)
    {
        var parts = new List<string> { "operator_id = @operatorId" };
        var p     = new Dapper.DynamicParameters();
        p.Add("operatorId", operatorId);

        if (f is null) return (string.Join(" AND ", parts), p);

        if (!string.IsNullOrEmpty(f.VehicleType))
            { parts.Add("vehicle_type = @vehicleType::public.vehicle_type"); p.Add("vehicleType", f.VehicleType); }
        if (!string.IsNullOrEmpty(f.Condition))
            { parts.Add("condition = @condition::public.vehicle_condition"); p.Add("condition", f.Condition); }
        if (!string.IsNullOrEmpty(f.Fuel))
            { parts.Add("fuel = @fuel::public.fuel_type"); p.Add("fuel", f.Fuel); }
        if (f.ProntaConsegna.HasValue)
            { parts.Add("pronta_consegna = @pc"); p.Add("pc", f.ProntaConsegna.Value); }
        if (f.IsNuovoArrivo.HasValue)
            { parts.Add("is_nuovo_arrivo = @na"); p.Add("na", f.IsNuovoArrivo.Value); }
        if (f.MinPrice.HasValue)
            { parts.Add("price >= @minPrice"); p.Add("minPrice", f.MinPrice.Value); }
        if (f.MaxPrice.HasValue)
            { parts.Add("price <= @maxPrice"); p.Add("maxPrice", f.MaxPrice.Value); }
        if (f.MaxMileageKm.HasValue)
            { parts.Add("mileage_km <= @maxMileage"); p.Add("maxMileage", f.MaxMileageKm.Value); }
        if (f.MinYear.HasValue)
            { parts.Add("registration_year >= @minYear"); p.Add("minYear", f.MinYear.Value); }
        if (f.MaxYear.HasValue)
            { parts.Add("registration_year <= @maxYear"); p.Add("maxYear", f.MaxYear.Value); }
        if (f.MinMonth.HasValue)
            { parts.Add("registration_month >= @minMonth"); p.Add("minMonth", f.MinMonth.Value); }
        if (f.MaxMonth.HasValue)
            { parts.Add("registration_month <= @maxMonth"); p.Add("maxMonth", f.MaxMonth.Value); }
        if (f.BranchId.HasValue)
            { parts.Add("branch_id = @branchId"); p.Add("branchId", f.BranchId.Value); }
        if (!string.IsNullOrEmpty(f.Transmission))
            { parts.Add("transmission::text = @transmission"); p.Add("transmission", f.Transmission); }
        if (f.VatDeductible.HasValue)
            { parts.Add("vat_deductible = @vatDed"); p.Add("vatDed", f.VatDeductible.Value); }
        if (f.HandicapAccessible.HasValue)
            { parts.Add("handicap_accessible = @handi"); p.Add("handi", f.HandicapAccessible.Value); }
        if (f.Imported.HasValue)
            { parts.Add("imported = @imported"); p.Add("imported", f.Imported.Value); }
        if (f.ForSale.HasValue)
            { parts.Add("for_sale = @forSale"); p.Add("forSale", f.ForSale.Value); }
        if (f.ForRental.HasValue)
            { parts.Add("for_rental = @forRental"); p.Add("forRental", f.ForRental.Value); }
        if (!string.IsNullOrWhiteSpace(f.Search))
            { parts.Add("(brand_name ILIKE @search OR model ILIKE @search)"); p.Add("search", $"%{f.Search.Trim()}%"); }

        return (string.Join(" AND ", parts), p);
    }

    private static string BuildOrderBy(VehicleFilter? _) => "created_at DESC";

    public async Task<IReadOnlyList<VehicleCard>> GetCardsByIdsAsync(
        Guid operatorId, IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        const string sql = """
            SELECT pvc.id, pvc.operator_id, pvc.operator_slug, pvc.operator_code,
                   pvc.branch_id, pvc.internal_code,
                   pvc.vehicle_type::text AS vehicle_type,
                   pvc.brand_name, pvc.brand_slug, pvc.model, pvc.version, pvc.body_type_name,
                   pvc.condition::text AS condition,
                   pvc.usage_type::text AS usage_type,
                   pvc.fuel::text AS fuel,
                   pvc.transmission::text AS transmission,
                   pvc.horsepower_cv, pvc.power_kw, pvc.engine_capacity_cc,
                   pvc.registration_month, pvc.registration_year, pvc.mileage_km, pvc.doors, pvc.seats,
                   pvc.color, pvc.emission_class, pvc.damaged,
                   pvc.price, pvc.previous_price, pvc.currency,
                   pvc.vat_deductible, pvc.imported, pvc.handicap_accessible,
                   pvc.for_sale, pvc.for_rental, pvc.rental_only,
                   pvc.rental_price, pvc.rental_weekly_price, pvc.rental_weekend_price,
                   pvc.is_sold, pvc.show_as_sold, pvc.pronta_consegna, pvc.is_nuovo_arrivo, pvc.nuovo_arrivo_until,
                   pvc.description, pvc.cover_image_url, pvc.cover_bucket, pvc.cover_storage_path,
                   pvc.branch_name, pvc.city, pvc.province, pvc.created_at, pvc.updated_at
            FROM public.public_vehicle_cards pvc
            JOIN unnest(@ids::uuid[]) WITH ORDINALITY AS t(id, ord) ON t.id = pvc.id
            WHERE pvc.operator_id = @operatorId
            ORDER BY t.ord
            """;
        using var conn = _factory.CreateConnection();
        var cards = await conn.QueryAsync<VehicleCard>(sql, new { ids = ids.ToArray(), operatorId });
        return cards.ToList();
    }
}
