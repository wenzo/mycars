using Dapper;

namespace MyCars.Repositories.Postgres;

public sealed class PostgresLeadRepository : ILeadRepository
{
    private readonly IDbConnectionFactory _factory;
    public PostgresLeadRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<VehicleLead> CreateAsync(VehicleLead lead)
    {
        const string sql = """
            INSERT INTO public.vehicle_leads
                (operator_id, vehicle_id, branch_id, full_name, email, phone, message,
                 privacy_accepted, marketing_accepted, source,
                 status, lead_type, preferred_date, preferred_time, tracking_code)
            VALUES
                (@OperatorId, @VehicleId, @BranchId, @FullName, @Email, @Phone, @Message,
                 @PrivacyAccepted, @MarketingAccepted, @Source,
                 @Status::public.lead_status, @LeadType::public.lead_type,
                 @PreferredDate, @PreferredTime, @TrackingCode)
            RETURNING id, operator_id, vehicle_id, branch_id, full_name, email, phone,
                      message, privacy_accepted, marketing_accepted, source,
                      status::text AS status, lead_type::text AS lead_type,
                      preferred_date, preferred_time, created_at, updated_at, tracking_code
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstAsync<VehicleLead>(sql, lead);
    }

    public async Task<VehicleLead?> GetByTrackingCodeAsync(Guid operatorId, string code)
    {
        const string sql = """
            SELECT id, operator_id, vehicle_id, full_name,
                   status::text AS status, lead_type::text AS lead_type,
                   preferred_date, preferred_time, created_at, tracking_code
            FROM public.vehicle_leads
            WHERE operator_id = @operatorId AND tracking_code = @code
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<VehicleLead>(sql, new { operatorId, code });
    }

    public async Task<PagedResult<VehicleLead>> GetByOperatorAsync(
        Guid operatorId, string? status, PageRequest page)
    {
        var where = "operator_id = @operatorId" +
                    (status is not null ? " AND status = @status::public.lead_status" : "");

        var countSql = $"SELECT COUNT(*) FROM public.vehicle_leads WHERE {where}";
        var itemsSql = $"""
            SELECT id, operator_id, vehicle_id, branch_id,
                   full_name, email, phone, message,
                   privacy_accepted, marketing_accepted, source,
                   status::text AS status, lead_type::text AS lead_type,
                   preferred_date, preferred_time, created_at, updated_at
            FROM public.vehicle_leads
            WHERE {where}
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset
            """;
        var param = new { operatorId, status, pageSize = page.PageSize, offset = page.Page * page.PageSize };
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, param);
        var items = (await conn.QueryAsync<VehicleLead>(itemsSql, param)).AsList();
        return new PagedResult<VehicleLead>(items, total);
    }

    public async Task<VehicleLead?> GetByIdAsync(Guid id, Guid operatorId)
    {
        const string sql = """
            SELECT id, operator_id, vehicle_id, branch_id,
                   full_name, email, phone, message,
                   privacy_accepted, marketing_accepted, source,
                   status::text AS status, lead_type::text AS lead_type,
                   preferred_date, preferred_time, created_at, updated_at
            FROM public.vehicle_leads
            WHERE id = @id AND operator_id = @operatorId
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<VehicleLead>(sql, new { id, operatorId });
    }

    public async Task<bool> UpdateStatusAsync(Guid id, Guid operatorId, string status)
    {
        const string sql = """
            UPDATE public.vehicle_leads
            SET status = @status::public.lead_status
            WHERE id = @id AND operator_id = @operatorId
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(sql, new { id, operatorId, status }) > 0;
    }

    public async Task<int> CountOpenAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? "SELECT COUNT(*) FROM public.vehicle_leads WHERE operator_id = @id AND status = 'new'"
            : "SELECT COUNT(*) FROM public.vehicle_leads WHERE status = 'new'";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, operatorId.HasValue ? new { id = operatorId } : null);
    }

    public async Task<int> CountTestDrivePendingAsync(Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? "SELECT COUNT(*) FROM public.vehicle_leads WHERE operator_id = @id AND lead_type = 'test_drive' AND status = 'new'"
            : "SELECT COUNT(*) FROM public.vehicle_leads WHERE lead_type = 'test_drive' AND status = 'new'";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, operatorId.HasValue ? new { id = operatorId } : null);
    }

    public async Task<PagedResult<VehicleLead>> GetAllAsync(
        Guid operatorId, PageRequest page, string? status = null, string? leadType = null)
    {
        var parts = new List<string> { "operator_id = @operatorId" };
        var p     = new DynamicParameters();
        p.Add("operatorId", operatorId);

        if (!string.IsNullOrEmpty(status))
            { parts.Add("status = @status::public.lead_status"); p.Add("status", status); }
        if (!string.IsNullOrEmpty(leadType))
            { parts.Add("lead_type = @leadType::public.lead_type"); p.Add("leadType", leadType); }

        p.Add("pageSize", page.PageSize);
        p.Add("offset",   page.Page * page.PageSize);

        var where    = "WHERE " + string.Join(" AND ", parts);
        var countSql = $"SELECT COUNT(*) FROM public.vehicle_leads {where}";
        var itemsSql = $"""
            SELECT id, operator_id, vehicle_id, full_name, email, phone,
                   status::text AS status, lead_type::text AS lead_type,
                   preferred_date, preferred_time, created_at
            FROM public.vehicle_leads
            {where}
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset
            """;
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(countSql, p);
        var items = (await conn.QueryAsync<VehicleLead>(itemsSql, p)).AsList();
        return new PagedResult<VehicleLead>(items, total);
    }

    public async Task<IReadOnlyList<VehicleLead>> GetRecentAsync(int count, Guid? operatorId = null)
    {
        var sql = operatorId.HasValue
            ? """
              SELECT id, operator_id, vehicle_id, full_name, email, phone,
                     status::text AS status, lead_type::text AS lead_type,
                     created_at
              FROM public.vehicle_leads
              WHERE operator_id = @id
              ORDER BY created_at DESC LIMIT @count
              """
            : """
              SELECT id, operator_id, vehicle_id, full_name, email, phone,
                     status::text AS status, lead_type::text AS lead_type,
                     created_at
              FROM public.vehicle_leads
              ORDER BY created_at DESC LIMIT @count
              """;
        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<VehicleLead>(sql,
            operatorId.HasValue ? new { id = operatorId, count } : (object)new { count })).AsList();
    }
}
