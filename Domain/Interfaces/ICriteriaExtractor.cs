using MyCars.Domain.Models;

namespace MyCars.Domain.Interfaces;

public interface ICriteriaExtractor
{
    // Restituisce i criteri estratti, o null se l'estrazione fallisce
    // (il chiamante ricade sulla ricerca per parole chiave).
    Task<SearchCriteria?> ExtractAsync(string userQuery, CancellationToken ct);
}
