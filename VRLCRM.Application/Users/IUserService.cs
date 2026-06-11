using VRLCRM.Domain.Entities;

namespace VRLCRM.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<bool> CreateUserAsync(ApplicationUser user, string password, string role, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default);
}
