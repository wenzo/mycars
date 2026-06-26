namespace MyCars.Domain.Repositories;

public interface IVehicleRepository
{
    Task<PagedResult<VehicleCard>>   GetPublicCardsAsync(Guid operatorId, PageRequest page, VehicleFilter? filter = null);
    Task<VehicleCard?>               GetCardByIdAsync(Guid id, Guid operatorId);
    Task<IReadOnlyList<VehicleCard>> GetCardsByIdsAsync(Guid operatorId, IReadOnlyList<Guid> ids, CancellationToken ct = default);

    // Admin
    Task<PagedResult<Vehicle>>     GetByOperatorAsync(Guid operatorId, PageRequest page);
    Task<Vehicle?>                 GetByIdAsync(Guid id, Guid operatorId);
    Task<int>                      CountActiveAsync(Guid? operatorId = null);
    Task<int>                      CountNuoviArriviAsync(Guid? operatorId = null);
    Task<int>                      CountProntaConsegnaAsync(Guid? operatorId = null);
    Task<IReadOnlyList<Vehicle>>   GetRecentAsync(int count, Guid? operatorId = null);
    Task<PagedResult<Vehicle>>     GetAllAsync(Guid operatorId, PageRequest page, string? condition = null, bool? isPublished = null, bool? isNuovoArrivo = null, bool? prontaConsegna = null, bool? vatDeductible = null, bool? handicapAccessible = null, bool? imported = null, bool? forSale = null, bool? forRental = null);
    Task<Vehicle?>                 FindByTargaAsync(string targa, Guid operatorId);

    // CRUD
    Task<IReadOnlyList<BrandInfo>> GetBrandsAsync();
    Task<IReadOnlyList<BrandInfo>> GetBrandsWithTypesAsync();
    Task<BrandInfo>                CreateBrandAsync(string name, string[] vehicleTypes);
    Task<BrandInfo?>               UpdateBrandAsync(Guid id, string name, string[] vehicleTypes);
    Task<bool>                     DeleteBrandAsync(Guid id);
    Task<Vehicle>                  CreateAsync(Vehicle vehicle, string brandName);
    Task<Vehicle?>                 UpdateAsync(Vehicle vehicle, string brandName);
    Task<bool>                     DeleteAsync(Guid id, Guid operatorId);

    // Images
    Task                              UpdateCoverAsync(Guid vehicleId, Guid operatorId, string? coverUrl);
    Task<IReadOnlyList<VehicleImage>> GetImagesAsync(Guid vehicleId, Guid operatorId);
    Task<VehicleImage>                AddImageAsync(VehicleImage image);
    Task<bool>                        DeleteImageAsync(Guid imageId, Guid vehicleId, Guid operatorId);
}
