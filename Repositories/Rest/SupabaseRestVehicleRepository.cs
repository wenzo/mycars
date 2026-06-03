namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestVehicleRepository : IVehicleRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestVehicleRepository(ISupabaseRestClient db) => _db = db;

    public async Task<PagedResult<VehicleCard>> GetPublicCardsAsync(
        Guid operatorId, PageRequest page, VehicleFilter? filter = null)
    {
        var f = BuildVehicleFilter(operatorId, filter);
        var total = await _db.CountAsync("public_vehicle_cards", f);
        var items = await _db.SelectAsync<VehicleCard>(
            "public_vehicle_cards", f,
            order:  "created_at.desc",
            limit:  page.PageSize,
            offset: page.Page * page.PageSize);
        return new PagedResult<VehicleCard>(items, total);
    }

    public Task<VehicleCard?> GetCardByIdAsync(Guid id, Guid operatorId)
        => _db.SelectOneAsync<VehicleCard>("public_vehicle_cards",
            $"id=eq.{id}&operator_id=eq.{operatorId}");

    public async Task<PagedResult<Vehicle>> GetByOperatorAsync(Guid operatorId, PageRequest page)
    {
        var f = $"operator_id=eq.{operatorId}&deleted_at=is.null";
        var total = await _db.CountAsync("vehicles", f);
        var items = await _db.SelectAsync<Vehicle>(
            "vehicles", f, order: "created_at.desc",
            limit: page.PageSize, offset: page.Page * page.PageSize);
        return new PagedResult<Vehicle>(items, total);
    }

    public Task<Vehicle?> GetByIdAsync(Guid id, Guid operatorId)
        => _db.SelectOneAsync<Vehicle>("vehicles",
            $"id=eq.{id}&operator_id=eq.{operatorId}&deleted_at=is.null");

    public Task<int> CountActiveAsync(Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&is_published=eq.true&deleted_at=is.null"
            : "is_published=eq.true&deleted_at=is.null";
        return _db.CountAsync("vehicles", f).ContinueWith(t => (int)t.Result);
    }

    public Task<int> CountNuoviArriviAsync(Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&is_nuovo_arrivo=eq.true&deleted_at=is.null"
            : "is_nuovo_arrivo=eq.true&deleted_at=is.null";
        return _db.CountAsync("vehicles", f).ContinueWith(t => (int)t.Result);
    }

    public Task<int> CountProntaConsegnaAsync(Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&pronta_consegna=eq.true&deleted_at=is.null"
            : "pronta_consegna=eq.true&deleted_at=is.null";
        return _db.CountAsync("vehicles", f).ContinueWith(t => (int)t.Result);
    }

    public async Task<PagedResult<Vehicle>> GetAllAsync(
        Guid operatorId, PageRequest page, string? condition = null, bool? isPublished = null,
        bool? isNuovoArrivo = null, bool? prontaConsegna = null)
    {
        var parts = new List<string> { $"operator_id=eq.{operatorId}", "deleted_at=is.null" };
        if (!string.IsNullOrEmpty(condition))  parts.Add($"condition=eq.{condition}");
        if (isPublished.HasValue)   parts.Add($"is_published=eq.{isPublished.Value.ToString().ToLower()}");
        if (isNuovoArrivo.HasValue) parts.Add($"is_nuovo_arrivo=eq.{isNuovoArrivo.Value.ToString().ToLower()}");
        if (prontaConsegna.HasValue) parts.Add($"pronta_consegna=eq.{prontaConsegna.Value.ToString().ToLower()}");

        var f     = string.Join("&", parts);
        var total = await _db.CountAsync("vehicles", f);
        var items = await _db.SelectAsync<Vehicle>(
            "vehicles", f, order: "created_at.desc",
            limit: page.PageSize, offset: page.Page * page.PageSize);
        return new PagedResult<Vehicle>(items, total);
    }

    public Task<Vehicle?> FindByTargaAsync(string targa, Guid operatorId)
        => _db.SelectOneAsync<Vehicle>("vehicles",
            $"targa=eq.{Uri.EscapeDataString(targa)}&operator_id=eq.{operatorId}&deleted_at=is.null",
            select: "id,operator_id,model,version,internal_code,targa");

    public Task<IReadOnlyList<BrandInfo>> GetBrandsAsync()
        => _db.SelectAsync<BrandInfo>("brands", order: "name.asc", select: "id,name,slug");

    public async Task<Vehicle> CreateAsync(Vehicle vehicle, string brandName)
    {
        vehicle.Id        = Guid.NewGuid();
        vehicle.BrandId   = await ResolveBrandAsync(brandName, vehicle.VehicleType);
        vehicle.CreatedAt = DateTimeOffset.UtcNow;
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _db.InsertAsync<Vehicle>("vehicles", new
        {
            id               = vehicle.Id,
            operator_id      = vehicle.OperatorId,
            branch_id        = vehicle.BranchId,
            internal_code    = vehicle.InternalCode,
            targa            = vehicle.Targa,
            vehicle_type     = vehicle.VehicleType,
            brand_id         = vehicle.BrandId,
            model            = vehicle.Model,
            version          = vehicle.Version,
            condition        = vehicle.Condition,
            fuel             = vehicle.Fuel,
            transmission     = vehicle.Transmission,
            horsepower_cv    = vehicle.HorsepowerCv,
            registration_year = vehicle.RegistrationYear,
            mileage_km       = vehicle.MileageKm,
            color            = vehicle.Color,
            price            = vehicle.Price,
            previous_price   = vehicle.PreviousPrice,
            negotiable       = vehicle.Negotiable,
            is_published     = vehicle.IsPublished,
            published_at     = vehicle.PublishedAt,
            pronta_consegna  = vehicle.ProntaConsegna,
            is_nuovo_arrivo  = vehicle.IsNuovoArrivo,
            description      = vehicle.Description,
            created_at       = vehicle.CreatedAt,
            updated_at       = vehicle.UpdatedAt,
        });
        return result ?? vehicle;
    }

    public async Task<Vehicle?> UpdateAsync(Vehicle vehicle, string brandName)
    {
        vehicle.BrandId   = await ResolveBrandAsync(brandName, vehicle.VehicleType);
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        return await _db.UpdateAsync<Vehicle>("vehicles",
            $"id=eq.{vehicle.Id}&operator_id=eq.{vehicle.OperatorId}&deleted_at=is.null", new
        {
            branch_id        = vehicle.BranchId,
            internal_code    = vehicle.InternalCode,
            targa            = vehicle.Targa,
            vehicle_type     = vehicle.VehicleType,
            brand_id         = vehicle.BrandId,
            model            = vehicle.Model,
            version          = vehicle.Version,
            condition        = vehicle.Condition,
            fuel             = vehicle.Fuel,
            transmission     = vehicle.Transmission,
            horsepower_cv    = vehicle.HorsepowerCv,
            registration_year = vehicle.RegistrationYear,
            mileage_km       = vehicle.MileageKm,
            color            = vehicle.Color,
            price            = vehicle.Price,
            previous_price   = vehicle.PreviousPrice,
            negotiable       = vehicle.Negotiable,
            is_published     = vehicle.IsPublished,
            pronta_consegna  = vehicle.ProntaConsegna,
            is_nuovo_arrivo  = vehicle.IsNuovoArrivo,
            description      = vehicle.Description,
            updated_at       = vehicle.UpdatedAt,
        });
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        await _db.UpdateAsync<object>("vehicles",
            $"id=eq.{id}&operator_id=eq.{operatorId}&deleted_at=is.null",
            new { deleted_at = DateTimeOffset.UtcNow, updated_at = DateTimeOffset.UtcNow });
        return true;
    }

    private async Task<Guid> ResolveBrandAsync(string brandName, string vehicleType)
    {
        var slug = System.Text.RegularExpressions.Regex.Replace(
            brandName.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

        var existing = await _db.SelectOneAsync<BrandInfo>("brands", $"slug=eq.{slug}", select: "id,name,slug");
        Guid brandId;
        if (existing is not null)
        {
            brandId = existing.Id;
        }
        else
        {
            var created = await _db.InsertAsync<BrandInfo>("brands", new { name = brandName.Trim(), slug });
            brandId = created!.Id;
        }

        var bvt = await _db.SelectOneAsync<BrandInfo>("brand_vehicle_types",
            $"brand_id=eq.{brandId}&vehicle_type=eq.{vehicleType}", select: "brand_id");
        if (bvt is null)
        {
            try { await _db.InsertAsync<object>("brand_vehicle_types",
                new { brand_id = brandId, vehicle_type = vehicleType }); }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict) { }
        }

        return brandId;
    }

    public Task<IReadOnlyList<Vehicle>> GetRecentAsync(int count, Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&deleted_at=is.null"
            : "deleted_at=is.null";
        return _db.SelectAsync<Vehicle>("vehicles", f, order: "created_at.desc", limit: count);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string BuildVehicleFilter(Guid operatorId, VehicleFilter? f)
    {
        var parts = new List<string> { $"operator_id=eq.{operatorId}" };
        if (f is null) return string.Join("&", parts);

        if (!string.IsNullOrEmpty(f.VehicleType))   parts.Add($"vehicle_type=eq.{f.VehicleType}");
        if (!string.IsNullOrEmpty(f.Condition))      parts.Add($"condition=eq.{f.Condition}");
        if (!string.IsNullOrEmpty(f.Fuel))           parts.Add($"fuel=eq.{f.Fuel}");
        if (f.ProntaConsegna.HasValue)  parts.Add($"pronta_consegna=eq.{f.ProntaConsegna.Value.ToString().ToLower()}");
        if (f.IsNuovoArrivo.HasValue)   parts.Add($"is_nuovo_arrivo=eq.{f.IsNuovoArrivo.Value.ToString().ToLower()}");
        if (f.MinPrice.HasValue)        parts.Add($"price=gte.{f.MinPrice}");
        if (f.MaxPrice.HasValue)        parts.Add($"price=lte.{f.MaxPrice}");
        if (f.MaxMileageKm.HasValue)    parts.Add($"mileage_km=lte.{f.MaxMileageKm}");
        if (f.MinYear.HasValue)         parts.Add($"registration_year=gte.{f.MinYear}");
        if (f.MaxYear.HasValue)         parts.Add($"registration_year=lte.{f.MaxYear}");
        if (f.BranchId.HasValue)        parts.Add($"branch_id=eq.{f.BranchId}");
        return string.Join("&", parts);
    }
}
