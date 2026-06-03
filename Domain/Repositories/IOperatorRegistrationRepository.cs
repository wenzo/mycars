namespace MyCars.Domain.Repositories;

public interface IOperatorRegistrationRepository
{
    Task<OperatorRegistration>              CreateAsync(OperatorRegistration reg);
    Task<IReadOnlyList<OperatorRegistration>> GetAllAsync(string? status = null);
    Task<OperatorRegistration?>             GetByIdAsync(Guid id);
    Task<bool>                              UpdateStatusAsync(Guid id, string status, string? reviewNotes);
}
