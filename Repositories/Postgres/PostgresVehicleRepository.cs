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
                   registration_month, registration_year, mileage_km,
                   price, previous_price, currency,
                   is_sold, show_as_sold, pronta_consegna, is_nuovo_arrivo, nuovo_arrivo_until,
                   description, cover_image_url, cover_bucket, cover_storage_path,
                   branch_name, city, province, created_at, updated_at
            FROM public.public_vehicle_cards
            WHERE {where}
            ORDER BY created_at DESC
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
                   v.horsepower_cv, v.registration_year, v.mileage_km,
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
                 horsepower_cv, registration_year, mileage_km, color,
                 price, previous_price, negotiable,
                 is_published, published_at, pronta_consegna, is_nuovo_arrivo, description,
                 created_at, updated_at)
            VALUES
                (@Id, @OperatorId, @BranchId, @InternalCode, @Targa,
                 @VehicleType::public.vehicle_type, @BrandId, @Model, @Version,
                 @Condition::public.vehicle_condition, @Fuel::public.fuel_type,
                 @Transmission::public.transmission_type,
                 @HorsepowerCv, @RegistrationYear, @MileageKm, @Color,
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
        bool? isNuovoArrivo = null, bool? prontaConsegna = null)
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
        if (f.BranchId.HasValue)
            { parts.Add("branch_id = @branchId"); p.Add("branchId", f.BranchId.Value); }

        return (string.Join(" AND ", parts), p);
    }
}
