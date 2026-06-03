namespace MyCars.Infrastructure.Storage;

public interface IFileStorageService
{
    /// <summary>Salva il file e restituisce l'URL pubblico relativo.</summary>
    Task<string> SaveAsync(IFormFile file, string folder, string fileName);

    void Delete(string relativePath);
}
