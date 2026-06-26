using System.Globalization;
using Dapper;
using MyCars.Domain.Repositories;
using MyCars.Infrastructure.Database;

namespace MyCars.Infrastructure.Repositories;

public sealed class VehicleEmbeddingRepository : IVehicleEmbeddingRepository
{
    private readonly IDbConnectionFactory _db;

    public VehicleEmbeddingRepository(IDbConnectionFactory db) => _db = db;

    // Serializza float[] nel formato testo accettato da pgvector: [0.1,-0.3,0.5,...]
    private static string ToVectorLiteral(float[] v) =>
        $"[{string.Join(",", v.Select(f => f.ToString("G8", CultureInfo.InvariantCulture)))}]";

    public async Task UpsertAsync(Guid vehicleId, Guid operatorId, float[] embedding, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO public.vehicle_embeddings (vehicle_id, operator_id, embedding, updated_at)
            VALUES (@vehicleId, @operatorId, @embedding::vector, now())
            ON CONFLICT (vehicle_id) DO UPDATE
              SET embedding   = EXCLUDED.embedding,
                  operator_id = EXCLUDED.operator_id,
                  updated_at  = now()
            """,
            new { vehicleId, operatorId, embedding = ToVectorLiteral(embedding) });
    }

    public async Task<IReadOnlyList<Guid>> SearchSimilarAsync(
        Guid operatorId, float[] queryEmbedding, string? vehicleType, int limit, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var ids = await conn.QueryAsync<Guid>("""
            SELECT ve.vehicle_id
            FROM public.vehicle_embeddings ve
            JOIN public.public_vehicle_cards pvc
              ON pvc.id = ve.vehicle_id
            WHERE ve.operator_id = @operatorId
              AND (@vehicleType IS NULL OR pvc.vehicle_type::text = @vehicleType)
            ORDER BY ve.embedding <=> @queryVector::vector
            LIMIT @limit
            """,
            new { operatorId, vehicleType, queryVector = ToVectorLiteral(queryEmbedding), limit });

        return ids.ToList();
    }

    public async Task DeleteAsync(Guid vehicleId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM public.vehicle_embeddings WHERE vehicle_id = @vehicleId",
            new { vehicleId });
    }
}
