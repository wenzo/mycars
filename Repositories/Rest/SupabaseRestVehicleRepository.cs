namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestVehicleRepository : IVehicleRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestVehicleRepository(ISupabaseRestClient db) => _db = db;

    // Tutti i campi del modello Vehicle ESCLUSO equipment (jsonb array non mappabile a string).
    // I campi JSONB del noleggio sono inclusi: restituiscono JsonElement? via System.Text.Json.
    private const string VehicleCols =
        "id,operator_id,branch_id,department_id," +
        "internal_code,external_code,vin,targa," +
        "vehicle_type,brand_id,model,version," +
        "body_type_id,usage_type,condition,fuel,transmission," +
        "engine_capacity_cc,horsepower_cv,power_kw," +
        "registration_month,registration_year,mileage_km," +
        "doors,seats,color,emission_class," +
        "handicap_accessible,vat_deductible,damaged,imported," +
        "for_sale,for_rental,rental_only," +
        "rental_price,rental_weekly_price,rental_weekend_price," +
        "rental_formulas,rental_redemption,rental_deposit_override,rental_vehicle_notes," +
        "description,price,previous_price,currency,listing_date," +
        "is_sold,show_as_sold,sold_at," +
        "pronta_consegna,is_nuovo_arrivo,nuovo_arrivo_until," +
        "is_published,published_at,sort_order," +
        "cover_image_url,created_at,updated_at,deleted_at";

    public async Task<PagedResult<VehicleCard>> GetPublicCardsAsync(
        Guid operatorId, PageRequest page, VehicleFilter? filter = null)
    {
        var f     = BuildVehicleFilter(operatorId, filter);
        var order = filter?.Sort switch
        {
            "prezzo_asc"  => "price.asc",
            "prezzo_desc" => "price.desc",
            "anno_desc"   => "registration_year.desc",
            "km_asc"      => "mileage_km.asc",
            _             => "created_at.desc"
        };
        var (items, total) = await _db.SelectWithCountAsync<VehicleCard>(
            "public_vehicle_cards", f,
            order:  order,
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
            "vehicles", f,
            select: VehicleCols,
            order:  "created_at.desc",
            limit:  page.PageSize,
            offset: page.Page * page.PageSize);
        return new PagedResult<Vehicle>(items, total);
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, Guid operatorId)
    {
        var vehicle = await _db.SelectOneAsync<Vehicle>("vehicles",
            $"id=eq.{id}&operator_id=eq.{operatorId}&deleted_at=is.null",
            select: VehicleCols);
        if (vehicle is null) return null;

        if (vehicle.BrandId != Guid.Empty)
        {
            var brand = await _db.SelectOneAsync<BrandInfo>("brands",
                $"id=eq.{vehicle.BrandId}", select: "id,name,slug");
            vehicle.BrandName = brand?.Name;
        }

        return vehicle;
    }

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
        bool? isNuovoArrivo = null, bool? prontaConsegna = null,
        bool? vatDeductible = null, bool? handicapAccessible = null,
        bool? imported = null, bool? forSale = null, bool? forRental = null)
    {
        var parts = new List<string> { $"operator_id=eq.{operatorId}", "deleted_at=is.null" };
        if (!string.IsNullOrEmpty(condition))    parts.Add($"condition=eq.{condition}");
        if (isPublished.HasValue)      parts.Add($"is_published=eq.{isPublished.Value.ToString().ToLower()}");
        if (isNuovoArrivo.HasValue)    parts.Add($"is_nuovo_arrivo=eq.{isNuovoArrivo.Value.ToString().ToLower()}");
        if (prontaConsegna.HasValue)   parts.Add($"pronta_consegna=eq.{prontaConsegna.Value.ToString().ToLower()}");
        if (vatDeductible.HasValue)    parts.Add($"vat_deductible=eq.{vatDeductible.Value.ToString().ToLower()}");
        if (handicapAccessible.HasValue) parts.Add($"handicap_accessible=eq.{handicapAccessible.Value.ToString().ToLower()}");
        if (imported.HasValue)         parts.Add($"imported=eq.{imported.Value.ToString().ToLower()}");
        if (forSale.HasValue)          parts.Add($"for_sale=eq.{forSale.Value.ToString().ToLower()}");
        if (forRental.HasValue)        parts.Add($"for_rental=eq.{forRental.Value.ToString().ToLower()}");

        var f     = string.Join("&", parts);
        var total = await _db.CountAsync("vehicles", f);
        var items = await _db.SelectAsync<Vehicle>(
            "vehicles", f,
            select: VehicleCols,
            order:  "created_at.desc",
            limit:  page.PageSize,
            offset: page.Page * page.PageSize);
        return new PagedResult<Vehicle>(items, total);
    }

    public Task<Vehicle?> FindByTargaAsync(string targa, Guid operatorId)
        => _db.SelectOneAsync<Vehicle>("vehicles",
            $"targa=eq.{Uri.EscapeDataString(targa)}&operator_id=eq.{operatorId}&deleted_at=is.null",
            select: "id,operator_id,model,version,internal_code,targa");

    public Task<IReadOnlyList<BrandInfo>> GetBrandsAsync()
        => _db.SelectAsync<BrandInfo>("brands", order: "name.asc", select: "id,name,slug");

    public async Task<IReadOnlyList<BrandInfo>> GetBrandsWithTypesAsync()
    {
        var brands = (await _db.SelectAsync<BrandInfo>("brands",
            select: "id,name,slug", order: "name.asc")).ToList();

        if (brands.Count == 0) return brands;

        var bvts = await _db.SelectAsync<BrandVehicleTypeRow>("brand_vehicle_types",
            select: "brand_id,vehicle_type");

        var typeMap = bvts.GroupBy(x => x.BrandId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.VehicleType).ToArray());

        foreach (var brand in brands)
            brand.VehicleTypes = typeMap.TryGetValue(brand.Id, out var t) ? t : [];

        return brands;
    }

    public async Task<BrandInfo> CreateBrandAsync(string name, string[] vehicleTypes)
    {
        var slug  = Slugify(name);
        var brand = await _db.InsertAsync<BrandInfo>("brands",
            new { name = name.Trim(), slug }, select: "id,name,slug");
        if (brand is null) throw new InvalidOperationException("Creazione marchio fallita.");

        foreach (var vt in vehicleTypes)
        {
            try { await _db.InsertAsync<object>("brand_vehicle_types", new { brand_id = brand.Id, vehicle_type = vt }); }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict) { }
        }

        brand.VehicleTypes = vehicleTypes;
        return brand;
    }

    public async Task<BrandInfo?> UpdateBrandAsync(Guid id, string name, string[] vehicleTypes)
    {
        var slug  = Slugify(name);
        var brand = await _db.UpdateAsync<BrandInfo>("brands", $"id=eq.{id}",
            new { name = name.Trim(), slug }, select: "id,name,slug");
        if (brand is null) return null;

        await _db.DeleteAsync("brand_vehicle_types", $"brand_id=eq.{id}");
        foreach (var vt in vehicleTypes)
        {
            try { await _db.InsertAsync<object>("brand_vehicle_types", new { brand_id = id, vehicle_type = vt }); }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict) { }
        }

        brand.VehicleTypes = vehicleTypes;
        return brand;
    }

    public async Task<bool> DeleteBrandAsync(Guid id)
    {
        await _db.DeleteAsync("brand_vehicle_types", $"brand_id=eq.{id}");
        await _db.DeleteAsync("brands", $"id=eq.{id}");
        return true;
    }

    private sealed class BrandVehicleTypeRow
    {
        public Guid   BrandId     { get; set; }
        public string VehicleType { get; set; } = "";
    }

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
            power_kw         = vehicle.PowerKw,
            registration_year = vehicle.RegistrationYear,
            mileage_km       = vehicle.MileageKm,
            color                = vehicle.Color,
            price                = vehicle.Price,
            previous_price       = vehicle.PreviousPrice,
            vat_deductible       = vehicle.VatDeductible,
            handicap_accessible  = vehicle.HandicapAccessible,
            imported             = vehicle.Imported,
            for_sale             = vehicle.ForSale,
            for_rental           = vehicle.ForRental,
            rental_price         = vehicle.RentalPrice,
            rental_weekly_price  = vehicle.RentalWeeklyPrice,
            rental_weekend_price = vehicle.RentalWeekendPrice,
            rental_formulas         = vehicle.RentalFormulas,
            rental_redemption       = vehicle.RentalRedemption,
            rental_deposit_override = vehicle.RentalDepositOverride,
            rental_vehicle_notes    = vehicle.RentalVehicleNotes,
            is_published         = vehicle.IsPublished,
            published_at         = vehicle.PublishedAt,
            pronta_consegna      = vehicle.ProntaConsegna,
            is_nuovo_arrivo      = vehicle.IsNuovoArrivo,
            description          = vehicle.Description,
            created_at           = vehicle.CreatedAt,
            updated_at           = vehicle.UpdatedAt,
        }, select: VehicleCols);
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
            power_kw         = vehicle.PowerKw,
            registration_year = vehicle.RegistrationYear,
            mileage_km       = vehicle.MileageKm,
            color                = vehicle.Color,
            price                = vehicle.Price,
            previous_price       = vehicle.PreviousPrice,
            vat_deductible       = vehicle.VatDeductible,
            handicap_accessible  = vehicle.HandicapAccessible,
            imported             = vehicle.Imported,
            for_sale             = vehicle.ForSale,
            for_rental           = vehicle.ForRental,
            rental_price         = vehicle.RentalPrice,
            rental_weekly_price  = vehicle.RentalWeeklyPrice,
            rental_weekend_price = vehicle.RentalWeekendPrice,
            rental_formulas         = vehicle.RentalFormulas,
            rental_redemption       = vehicle.RentalRedemption,
            rental_deposit_override = vehicle.RentalDepositOverride,
            rental_vehicle_notes    = vehicle.RentalVehicleNotes,
            is_published         = vehicle.IsPublished,
            pronta_consegna      = vehicle.ProntaConsegna,
            is_nuovo_arrivo      = vehicle.IsNuovoArrivo,
            description          = vehicle.Description,
            updated_at           = vehicle.UpdatedAt,
        }, select: VehicleCols);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        await _db.UpdateAsync<object>("vehicles",
            $"id=eq.{id}&operator_id=eq.{operatorId}&deleted_at=is.null",
            new { deleted_at = DateTimeOffset.UtcNow, updated_at = DateTimeOffset.UtcNow });
        return true;
    }

    // ── Images ────────────────────────────────────────────────────────────────

    public async Task UpdateCoverAsync(Guid vehicleId, Guid operatorId, string? coverUrl)
    {
        await _db.UpdateAsync<object>("vehicles",
            $"id=eq.{vehicleId}&operator_id=eq.{operatorId}&deleted_at=is.null",
            new { cover_image_url = coverUrl, updated_at = DateTimeOffset.UtcNow });
    }

    public Task<IReadOnlyList<VehicleImage>> GetImagesAsync(Guid vehicleId, Guid operatorId)
        => _db.SelectAsync<VehicleImage>("vehicle_images",
            $"vehicle_id=eq.{vehicleId}&operator_id=eq.{operatorId}",
            select: "id,vehicle_id,operator_id,url,sort_order,created_at",
            order:  "sort_order.asc,created_at.asc");

    public async Task<VehicleImage> AddImageAsync(VehicleImage image)
    {
        image.Id        = Guid.NewGuid();
        image.CreatedAt = DateTimeOffset.UtcNow;
        var result = await _db.InsertAsync<VehicleImage>("vehicle_images", new
        {
            id          = image.Id,
            vehicle_id  = image.VehicleId,
            operator_id = image.OperatorId,
            url         = image.Url,
            sort_order  = image.SortOrder,
            created_at  = image.CreatedAt,
        });
        return result ?? image;
    }

    public async Task<bool> DeleteImageAsync(Guid imageId, Guid vehicleId, Guid operatorId)
    {
        await _db.DeleteAsync("vehicle_images",
            $"id=eq.{imageId}&vehicle_id=eq.{vehicleId}&operator_id=eq.{operatorId}");
        return true;
    }

    public Task<IReadOnlyList<Vehicle>> GetRecentAsync(int count, Guid? operatorId = null)
    {
        var f = operatorId.HasValue
            ? $"operator_id=eq.{operatorId}&deleted_at=is.null"
            : "deleted_at=is.null";
        return _db.SelectAsync<Vehicle>("vehicles", f,
            select: VehicleCols,
            order:  "created_at.desc",
            limit:  count);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

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

    private async Task<Guid> ResolveBrandAsync(string brandName, string vehicleType)
    {
        var slug = Slugify(brandName);

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

    private static string BuildVehicleFilter(Guid operatorId, VehicleFilter? f)
    {
        var parts = new List<string> { $"operator_id=eq.{operatorId}" };
        if (f is null) return string.Join("&", parts);

        if (!string.IsNullOrEmpty(f.VehicleType))   parts.Add($"vehicle_type=eq.{f.VehicleType}");
        if (!string.IsNullOrEmpty(f.Condition))      parts.Add($"condition=eq.{f.Condition}");
        if (!string.IsNullOrEmpty(f.Fuel))           parts.Add($"fuel=eq.{f.Fuel}");
        if (!string.IsNullOrEmpty(f.Transmission))   parts.Add($"transmission=eq.{f.Transmission}");
        if (f.ProntaConsegna.HasValue)     parts.Add($"pronta_consegna=eq.{f.ProntaConsegna.Value.ToString().ToLower()}");
        if (f.IsNuovoArrivo.HasValue)      parts.Add($"is_nuovo_arrivo=eq.{f.IsNuovoArrivo.Value.ToString().ToLower()}");
        if (f.VatDeductible.HasValue)      parts.Add($"vat_deductible=eq.{f.VatDeductible.Value.ToString().ToLower()}");
        if (f.HandicapAccessible.HasValue) parts.Add($"handicap_accessible=eq.{f.HandicapAccessible.Value.ToString().ToLower()}");
        if (f.Imported.HasValue)           parts.Add($"imported=eq.{f.Imported.Value.ToString().ToLower()}");
        if (f.ForSale.HasValue)            parts.Add($"for_sale=eq.{f.ForSale.Value.ToString().ToLower()}");
        if (f.ForRental.HasValue)          parts.Add($"for_rental=eq.{f.ForRental.Value.ToString().ToLower()}");
        if (f.MinPrice.HasValue)           parts.Add($"price=gte.{f.MinPrice}");
        if (f.MaxPrice.HasValue)           parts.Add($"price=lte.{f.MaxPrice}");
        if (f.MaxMileageKm.HasValue)       parts.Add($"mileage_km=lte.{f.MaxMileageKm}");
        if (f.MinYear.HasValue)            parts.Add($"registration_year=gte.{f.MinYear}");
        if (f.MaxYear.HasValue)            parts.Add($"registration_year=lte.{f.MaxYear}");
        if (f.MinMonth.HasValue)           parts.Add($"registration_month=gte.{f.MinMonth}");
        if (f.MaxMonth.HasValue)           parts.Add($"registration_month=lte.{f.MaxMonth}");
        if (f.BranchId.HasValue)           parts.Add($"branch_id=eq.{f.BranchId}");
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var t = f.Search.Trim().Replace("*", "").Replace("(", "").Replace(")", "");
            parts.Add($"or=(model.ilike.*{t}*,brand_name.ilike.*{t}*)");
        }
        // Fuel: multi-valore AI (ha precedenza sul singolo)
        if (f.FuelTypes is { Count: > 0 })
            parts.Add($"fuel=in.({string.Join(",", f.FuelTypes)})");
        // BodyType: multi-valore AI
        if (f.BodyTypes is { Count: > 0 })
            parts.Add($"body_type_name=in.({string.Join(",", f.BodyTypes)})");
        if (f.MinSeats.HasValue)
            parts.Add($"seats=gte.{f.MinSeats}");

        return string.Join("&", parts);
    }
}
