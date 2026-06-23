namespace MyCars.Domain.Repositories;

public interface ILeadRepository
{
    Task<VehicleLead>                  CreateAsync(VehicleLead lead);
    Task<PagedResult<VehicleLead>>     GetByOperatorAsync(Guid operatorId, string? status, PageRequest page);
    Task<VehicleLead?>                 GetByIdAsync(Guid id, Guid operatorId);
    Task<bool>                         UpdateStatusAsync(Guid id, Guid operatorId, string status);

    Task<VehicleLead?>                 GetByTrackingCodeAsync(Guid operatorId, string code);

    // Admin
    Task<int>                          CountOpenAsync(Guid? operatorId = null);
    Task<int>                          CountTestDrivePendingAsync(Guid? operatorId = null);
    Task<IReadOnlyList<VehicleLead>>   GetRecentAsync(int count, Guid? operatorId = null);
    Task<PagedResult<VehicleLead>>     GetAllAsync(Guid operatorId, PageRequest page, string? status = null, string? leadType = null);
}
