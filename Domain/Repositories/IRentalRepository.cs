using MyCars.Domain.Models;

namespace MyCars.Domain.Repositories;

public interface IRentalRepository
{
    // Liste
    Task<PagedResult<Rental>> GetByOperatorAsync(Guid operatorId, PageRequest page, string? status = null);
    Task<IReadOnlyList<Rental>> GetActiveAsync(Guid operatorId);
    Task<IReadOnlyList<Rental>> GetReturningTodayAsync(Guid operatorId);
    Task<int> CountByStatusAsync(Guid operatorId, string status);

    // Singolo
    Task<Rental?> GetByIdAsync(Guid id, Guid operatorId);

    // Disponibilità
    Task<bool> IsAvailableAsync(Guid vehicleId, DateOnly startDate, DateOnly endDate, Guid? excludeId = null);

    // CRUD
    Task<Rental> CreateAsync(Rental rental);
    Task<Rental> UpdateAsync(Rental rental);

    // Transizioni di stato
    Task<bool> ActivateAsync(Guid id, Guid operatorId, int? kmDeparture, string? fuelDeparture);
    Task<bool> CloseAsync(Guid id, Guid operatorId, DateOnly actualEndDate, int? kmReturn, string? fuelReturn);
    Task<bool> CancelAsync(Guid id, Guid operatorId);

    // Foto
    Task<IReadOnlyList<RentalPhoto>> GetPhotosAsync(Guid rentalId, Guid operatorId);
    Task<RentalPhoto> AddPhotoAsync(RentalPhoto photo);
    Task<bool> DeletePhotoAsync(Guid photoId, Guid rentalId, Guid operatorId);
}
