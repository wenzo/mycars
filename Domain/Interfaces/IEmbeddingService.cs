namespace MyCars.Domain.Interfaces;

public interface IEmbeddingService
{
    bool IsConfigured { get; }

    // Restituisce null se il servizio non è configurato o la chiamata API fallisce.
    Task<float[]?> EmbedAsync(string text, CancellationToken ct = default);
}
