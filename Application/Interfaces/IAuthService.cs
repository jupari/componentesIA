using ComponentesIA.Application.DTOs;
using System.Threading.Tasks;

namespace ComponentesIA.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<bool> AssignRoleAsync(Guid userId, Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);

    // ── Read ──────────────────────────────────────────────────────────────
    Task<List<UserListItemDto>> GetUsersAsync();
    Task<List<RoleDto>> GetRolesAsync();
    Task<List<PermissionDto>> GetPermissionsAsync();

    // ── Write (roles) ─────────────────────────────────────────────────────
    Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);
    Task<bool> DeleteRoleAsync(Guid roleId);
    Task<bool> RevokePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> SetUserStatusAsync(Guid userId, bool isActive);
}
