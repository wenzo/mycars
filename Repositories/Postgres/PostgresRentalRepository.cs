using Dapper;
using MyCars.Domain.Models;
using MyCars.Domain.Repositories;
using MyCars.Infrastructure.Database;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresRentalRepository : IRentalRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresRentalRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string RentalSelect = """
        r.id, r.operator_id, r.vehicle_id,
        r.customer_name, r.customer_phone, r.customer_license, r.customer_fiscal_code,
        r.start_date, r.planned_end_date, r.actual_end_date,
        r.km_departure, r.km_return,
        r.fuel_departure::text  AS fuel_departure,
        r.fuel_return::text     AS fuel_return,
        r.agreed_price, r.deposit_amount, r.deposit_returned,
        r.payment_method::text  AS payment_method,
        r.is_paid,
        r.status::text          AS status,
        r.notes, r.created_at, r.updated_at,
        br.name AS brand_name, b.name AS brand_name, b.name AS vehicle_brand,
        v.model AS vehicle_model, v.targa AS vehicle_targa
        """;

    private static string WithVehicleJoin(string where, string order, string extra = "") => $"""
        SELECT r.id, r.operator_id, r.vehicle_id,
               r.customer_name, r.customer_phone, r.customer_license, r.customer_fiscal_code,
               r.start_date, r.planned_end_date, r.actual_end_date,
               r.km_departure, r.km_return,
               r.fuel_departure::text  AS fuel_departure,
               r.fuel_return::text     AS fuel_return,
               r.agreed_price, r.deposit_amount, r.deposit_returned,
               r.payment_method::text  AS payment_method,
               r.is_paid,
               r.status::text          AS status,
               r.notes, r.created_at, r.updated_at,
               b.name  AS vehicle_brand,
               v.model AS vehicle_model,
               v.targa AS vehicle_targa
        FROM   public.rentals r
        JOIN   public.vehicles v ON v.id = r.vehicle_id
        JOIN   public.brands   b ON b.id = v.brand_id
        WHERE  {where}
        ORDER  BY {order}
        {extra}
        """;

    // ── Liste ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<Rental>> GetByOperatorAsync(
        Guid operatorId, PageRequest page, string? status = null)
    {
        var param = new DynamicParameters();
        param.Add("opId", operatorId);
        var where = "r.operator_id = @opId";
        if (!string.IsNullOrWhiteSpace(status))
        {
            param.Add("status", status);
            where += " AND r.status::text = @status";
        }

        var countSql = $"SELECT COUNT(*) FROM public.rentals r WHERE {where}";
        param.Add("ps",  page.PageSize);
        param.Add("off", page.Page * page.PageSize);
        var itemsSql = WithVehicleJoin(where, "r.start_date DESC", "LIMIT @ps OFFSET @off");

        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, param);
        var items = (await conn.QueryAsync<Rental>(itemsSql, param)).AsList();
        return new PagedResult<Rental>(items, total);
    }

    public async Task<IReadOnlyList<Rental>> GetActiveAsync(Guid operatorId)
    {
        var sql = WithVehicleJoin("r.operator_id = @opId AND r.status = 'active'", "r.start_date ASC");
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<Rental>(sql, new { opId = operatorId })).AsList();
    }

    public async Task<IReadOnlyList<Rental>> GetReturningTodayAsync(Guid operatorId)
    {
        var sql = WithVehicleJoin(
            "r.operator_id = @opId AND r.status IN ('booked','active') AND r.planned_end_date = CURRENT_DATE",
            "r.start_date ASC");
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<Rental>(sql, new { opId = operatorId })).AsList();
    }

    public async Task<int> CountByStatusAsync(Guid operatorId, string status)
    {
        const string sql = "SELECT COUNT(*) FROM public.rentals WHERE operator_id = @opId AND status::text = @status";
        using var conn = _factory.CreateConnection();
        return (int)await conn.ExecuteScalarAsync<long>(sql, new { opId = operatorId, status });
    }

    // ── Singolo ───────────────────────────────────────────────────────────────

    public async Task<Rental?> GetByIdAsync(Guid id, Guid operatorId)
    {
        var sql = WithVehicleJoin("r.id = @id AND r.operator_id = @opId", "r.created_at DESC");
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Rental>(sql, new { id, opId = operatorId });
    }

    // ── Disponibilità ─────────────────────────────────────────────────────────

    public async Task<bool> IsAvailableAsync(
        Guid vehicleId, DateOnly startDate, DateOnly endDate, Guid? excludeId = null)
    {
        const string sql = """
            SELECT COUNT(*) FROM public.rentals
            WHERE vehicle_id = @vId
              AND status IN ('booked','active')
              AND (@excl IS NULL OR id != @excl)
              AND start_date <= @end
              AND planned_end_date >= @start
            """;
        using var conn = _factory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<long>(sql, new
        {
            vId   = vehicleId,
            start = startDate,
            end   = endDate,
            excl  = excludeId,
        });
        return count == 0;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public async Task<Rental> CreateAsync(Rental r)
    {
        const string sql = """
            INSERT INTO public.rentals
                (operator_id, vehicle_id,
                 customer_name, customer_phone, customer_license, customer_fiscal_code,
                 start_date, planned_end_date,
                 agreed_price, deposit_amount, payment_method, status, notes)
            VALUES
                (@OperatorId, @VehicleId,
                 @CustomerName, @CustomerPhone, @CustomerLicense, @CustomerFiscalCode,
                 @StartDate, @PlannedEndDate,
                 @AgreedPrice, @DepositAmount, @PaymentMethod::payment_method, 'booked', @Notes)
            RETURNING id, operator_id, vehicle_id,
                      customer_name, customer_phone, customer_license, customer_fiscal_code,
                      start_date, planned_end_date, actual_end_date,
                      km_departure, km_return,
                      fuel_departure::text, fuel_return::text,
                      agreed_price, deposit_amount, deposit_returned,
                      payment_method::text, is_paid,
                      status::text AS status, notes, created_at, updated_at
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryFirstAsync<Rental>(sql, r));
    }

    public async Task<Rental> UpdateAsync(Rental r)
    {
        const string sql = """
            UPDATE public.rentals SET
                customer_name        = @CustomerName,
                customer_phone       = @CustomerPhone,
                customer_license     = @CustomerLicense,
                customer_fiscal_code = @CustomerFiscalCode,
                start_date           = @StartDate,
                planned_end_date     = @PlannedEndDate,
                agreed_price         = @AgreedPrice,
                deposit_amount       = @DepositAmount,
                deposit_returned     = @DepositReturned,
                payment_method       = @PaymentMethod::payment_method,
                is_paid              = @IsPaid,
                notes                = @Notes
            WHERE id = @Id AND operator_id = @OperatorId
            RETURNING id, operator_id, vehicle_id,
                      customer_name, customer_phone, customer_license, customer_fiscal_code,
                      start_date, planned_end_date, actual_end_date,
                      km_departure, km_return,
                      fuel_departure::text, fuel_return::text,
                      agreed_price, deposit_amount, deposit_returned,
                      payment_method::text, is_paid,
                      status::text AS status, notes, created_at, updated_at
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryFirstAsync<Rental>(sql, r));
    }

    // ── Transizioni stato ─────────────────────────────────────────────────────

    public async Task<bool> ActivateAsync(
        Guid id, Guid operatorId, int? kmDeparture, string? fuelDeparture)
    {
        const string sql = """
            UPDATE public.rentals
            SET status         = 'active',
                km_departure   = @km,
                fuel_departure = @fuel::fuel_level
            WHERE id = @id AND operator_id = @opId AND status = 'booked'
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql,
            new { id, opId = operatorId, km = kmDeparture, fuel = fuelDeparture }) > 0;
    }

    public async Task<bool> CloseAsync(
        Guid id, Guid operatorId, DateOnly actualEndDate, int? kmReturn, string? fuelReturn)
    {
        const string sql = """
            UPDATE public.rentals
            SET status          = 'closed',
                actual_end_date = @actualEnd,
                km_return       = @km,
                fuel_return     = @fuel::fuel_level
            WHERE id = @id AND operator_id = @opId AND status = 'active'
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql,
            new { id, opId = operatorId, actualEnd = actualEndDate, km = kmReturn, fuel = fuelReturn }) > 0;
    }

    public async Task<bool> CancelAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            UPDATE public.rentals SET status = 'cancelled'
            WHERE id = @id AND operator_id = @opId AND status IN ('booked','active')
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, opId = operatorId }) > 0;
    }

    // ── Foto ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RentalPhoto>> GetPhotosAsync(Guid rentalId, Guid operatorId)
    {
        const string sql = """
            SELECT id, rental_id, operator_id, phase::text AS phase, url, created_at
            FROM public.rental_photos
            WHERE rental_id = @rId AND operator_id = @opId
            ORDER BY created_at
            """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<RentalPhoto>(sql, new { rId = rentalId, opId = operatorId })).AsList();
    }

    public async Task<RentalPhoto> AddPhotoAsync(RentalPhoto p)
    {
        const string sql = """
            INSERT INTO public.rental_photos (rental_id, operator_id, phase, url)
            VALUES (@RentalId, @OperatorId, @Phase::rental_photo_phase, @Url)
            RETURNING id, rental_id, operator_id, phase::text AS phase, url, created_at
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstAsync<RentalPhoto>(sql, p);
    }

    public async Task<bool> DeletePhotoAsync(Guid photoId, Guid rentalId, Guid operatorId)
    {
        const string sql = """
            DELETE FROM public.rental_photos
            WHERE id = @pid AND rental_id = @rId AND operator_id = @opId
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { pid = photoId, rId = rentalId, opId = operatorId }) > 0;
    }
}
