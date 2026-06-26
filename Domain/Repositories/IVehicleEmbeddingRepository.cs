namespace MyCars.Domain.Repositories;

public interface IVehicleEmbeddingRepository
{
    Task UpsertAsync(Guid vehicleId, Guid operatorId, float[] embedding, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> SearchSimilarAsync(Guid operatorId, float[] queryEmbedding, string? vehicleType, int limit, CancellationToken ct = default);
    Task DeleteAsync(Guid vehicleId, CancellationToken ct = default);
}
