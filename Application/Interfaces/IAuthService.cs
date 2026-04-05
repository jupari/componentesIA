using ComponentesIA.Application.DTOs;
using System.Threading.Tasks;

namespace ComponentesIA.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<bool> AssignRoleAsync(Guid userId, Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
}
