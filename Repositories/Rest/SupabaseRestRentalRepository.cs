using MyCars.Domain.Models;
using MyCars.Domain.Repositories;
using MyCars.Infrastructure.Http;

namespace MyCars.Repositories.Rest;

public sealed class SupabaseRestRentalRepository : IRentalRepository
{
    private readonly ISupabaseRestClient _db;
    public SupabaseRestRentalRepository(ISupabaseRestClient db) => _db = db;

    private const string RentalCols =
        "id,operator_id,vehicle_id," +
        "customer_name,customer_phone,customer_license,customer_fiscal_code," +
        "start_date,planned_end_date,actual_end_date," +
        "km_departure,km_return,fuel_departure,fuel_return," +
        "agreed_price,deposit_amount,deposit_returned,payment_method,is_paid," +
        "status,notes,created_at,updated_at";

    // ── Liste ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<Rental>> GetByOperatorAsync(
        Guid operatorId, PageRequest page, string? status = null)
    {
        var f = $"operator_id=eq.{operatorId}";
        if (!string.IsNullOrWhiteSpace(status))
            f += $"&status=eq.{status}";

        var (items, total) = await _db.SelectWithCountAsync<Rental>(
            "rentals", f,
            select: RentalCols,
            order:  "start_date.desc",
            limit:  page.PageSize,
            offset: page.Page * page.PageSize);

        await EnrichWithVehicleInfoAsync(items);
        return new PagedResult<Rental>(items, total);
    }

    public async Task<IReadOnlyList<Rental>> GetActiveAsync(Guid operatorId)
    {
        var items = await _db.SelectAsync<Rental>(
            "rentals",
            $"operator_id=eq.{operatorId}&status=eq.active",
            select: RentalCols,
            order: "start_date.asc");
        await EnrichWithVehicleInfoAsync(items);
        return items;
    }

    public async Task<IReadOnlyList<Rental>> GetReturningTodayAsync(Guid operatorId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var items = await _db.SelectAsync<Rental>(
            "rentals",
            $"operator_id=eq.{operatorId}&status=in.(booked,active)&planned_end_date=eq.{today}",
            select: RentalCols,
            order: "start_date.asc");
        await EnrichWithVehicleInfoAsync(items);
        return items;
    }

    public async Task<int> CountByStatusAsync(Guid operatorId, string status)
        => (int)await _db.CountAsync("rentals", $"operator_id=eq.{operatorId}&status=eq.{status}");

    // ── Singolo ───────────────────────────────────────────────────────────────

    public async Task<Rental?> GetByIdAsync(Guid id, Guid operatorId)
    {
        var rental = await _db.SelectOneAsync<Rental>(
            "rentals",
            $"id=eq.{id}&operator_id=eq.{operatorId}",
            select: RentalCols);
        if (rental is not null)
            await EnrichWithVehicleInfoAsync([rental]);
        return rental;
    }

    // ── Disponibilità ─────────────────────────────────────────────────────────

    public async Task<bool> IsAvailableAsync(
        Guid vehicleId, DateOnly startDate, DateOnly endDate, Guid? excludeId = null)
    {
        var start = startDate.ToString("yyyy-MM-dd");
        var end   = endDate.ToString("yyyy-MM-dd");
        var f = $"vehicle_id=eq.{vehicleId}&status=in.(booked,active)" +
                $"&start_date=lte.{end}&planned_end_date=gte.{start}";
        if (excludeId.HasValue)
            f += $"&id=neq.{excludeId}";
        var count = await _db.CountAsync("rentals", f);
        return count == 0;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public async Task<Rental> CreateAsync(Rental r)
    {
        var payload = new
        {
            operator_id          = r.OperatorId,
            vehicle_id           = r.VehicleId,
            customer_name        = r.CustomerName,
            customer_phone       = r.CustomerPhone,
            customer_license     = r.CustomerLicense,
            customer_fiscal_code = r.CustomerFiscalCode,
            start_date           = r.StartDate.ToString("yyyy-MM-dd"),
            planned_end_date     = r.PlannedEndDate.ToString("yyyy-MM-dd"),
            agreed_price         = r.AgreedPrice,
            deposit_amount       = r.DepositAmount,
            payment_method       = r.PaymentMethod,
            status               = "booked",
            notes                = r.Notes,
        };
        var created = await _db.InsertAsync<Rental>("rentals", payload, select: RentalCols);
        return created!;
    }

    public async Task<Rental> UpdateAsync(Rental r)
    {
        var payload = new
        {
            customer_name        = r.CustomerName,
            customer_phone       = r.CustomerPhone,
            customer_license     = r.CustomerLicense,
            customer_fiscal_code = r.CustomerFiscalCode,
            start_date           = r.StartDate.ToString("yyyy-MM-dd"),
            planned_end_date     = r.PlannedEndDate.ToString("yyyy-MM-dd"),
            agreed_price         = r.AgreedPrice,
            deposit_amount       = r.DepositAmount,
            deposit_returned     = r.DepositReturned,
            payment_method       = r.PaymentMethod,
            is_paid              = r.IsPaid,
            notes                = r.Notes,
        };
        var updated = await _db.UpdateAsync<Rental>(
            "rentals", $"id=eq.{r.Id}&operator_id=eq.{r.OperatorId}",
            payload, select: RentalCols);
        return updated!;
    }

    // ── Transizioni stato ─────────────────────────────────────────────────────

    public async Task<bool> ActivateAsync(
        Guid id, Guid operatorId, int? kmDeparture, string? fuelDeparture)
    {
        var payload = new
        {
            status         = "active",
            km_departure   = kmDeparture,
            fuel_departure = fuelDeparture,
        };
        var result = await _db.UpdateAsync<Rental>(
            "rentals",
            $"id=eq.{id}&operator_id=eq.{operatorId}&status=eq.booked",
            payload, select: "id");
        return result is not null;
    }

    public async Task<bool> CloseAsync(
        Guid id, Guid operatorId, DateOnly actualEndDate, int? kmReturn, string? fuelReturn)
    {
        var payload = new
        {
            status          = "closed",
            actual_end_date = actualEndDate.ToString("yyyy-MM-dd"),
            km_return       = kmReturn,
            fuel_return     = fuelReturn,
        };
        var result = await _db.UpdateAsync<Rental>(
            "rentals",
            $"id=eq.{id}&operator_id=eq.{operatorId}&status=eq.active",
            payload, select: "id");
        return result is not null;
    }

    public async Task<bool> CancelAsync(Guid id, Guid operatorId)
    {
        var result = await _db.UpdateAsync<Rental>(
            "rentals",
            $"id=eq.{id}&operator_id=eq.{operatorId}&status=in.(booked,active)",
            new { status = "cancelled" }, select: "id");
        return result is not null;
    }

    // ── Foto ──────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<RentalPhoto>> GetPhotosAsync(Guid rentalId, Guid operatorId)
        => _db.SelectAsync<RentalPhoto>(
            "rental_photos",
            $"rental_id=eq.{rentalId}&operator_id=eq.{operatorId}",
            order: "created_at.asc");

    public async Task<RentalPhoto> AddPhotoAsync(RentalPhoto p)
    {
        var payload = new
        {
            rental_id   = p.RentalId,
            operator_id = p.OperatorId,
            phase       = p.Phase,
            url         = p.Url,
        };
        var created = await _db.InsertAsync<RentalPhoto>("rental_photos", payload);
        return created!;
    }

    public async Task<bool> DeletePhotoAsync(Guid photoId, Guid rentalId, Guid operatorId)
    {
        await _db.DeleteAsync("rental_photos",
            $"id=eq.{photoId}&rental_id=eq.{rentalId}&operator_id=eq.{operatorId}");
        return true;
    }

    // ── Enrichment ────────────────────────────────────────────────────────────

    private async Task EnrichWithVehicleInfoAsync(IReadOnlyList<Rental> rentals)
    {
        foreach (var r in rentals)
        {
            var v = await _db.SelectOneAsync<VehicleStub>(
                "vehicles",
                $"id=eq.{r.VehicleId}",
                select: "id,brand_id,model,targa");
            if (v is not null)
            {
                r.VehicleModel = v.Model;
                r.VehicleTarga = v.Targa;
                if (v.BrandId != Guid.Empty)
                {
                    var brand = await _db.SelectOneAsync<BrandInfo>(
                        "brands", $"id=eq.{v.BrandId}", select: "id,name");
                    r.VehicleBrand = brand?.Name;
                }
            }
        }
    }

    private sealed class VehicleStub
    {
        public Guid    Id      { get; set; }
        public Guid    BrandId { get; set; }
        public string  Model   { get; set; } = "";
        public string? Targa   { get; set; }
    }
}
