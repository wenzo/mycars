namespace MyCars.Domain.Repositories;

public interface IOperatorUserRepository
{
    Task<OperatorUser?> GetByEmailAsync(string email);
    Task<OperatorUser?> GetByIdAsync(Guid id);
    Task UpdateLastLoginAsync(Guid id);
    Task<OperatorUser>  CreateAsync(OperatorUser user);
}
