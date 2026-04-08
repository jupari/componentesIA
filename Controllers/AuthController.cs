using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ComponentesIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }

    [HttpPost("assign-role")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId)
    {
        var ok = await _authService.AssignRoleAsync(userId, roleId);
        if (!ok) return BadRequest();
        return Ok();
    }

    [HttpPost("assign-permission")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AssignPermission(Guid roleId, Guid permissionId)
    {
        var ok = await _authService.AssignPermissionToRoleAsync(roleId, permissionId);
        if (!ok) return BadRequest();
        return Ok();
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _authService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("roles")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _authService.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("permissions")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _authService.GetPermissionsAsync();
        return Ok(permissions);
    }

    // ── Roles write ──────────────────────────────────────────────────────────

    [HttpPost("roles")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        var role = await _authService.CreateRoleAsync(dto);
        return Ok(role);
    }

    [HttpDelete("roles/{roleId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        var ok = await _authService.DeleteRoleAsync(roleId);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> RevokePermission(Guid roleId, Guid permissionId)
    {
        var ok = await _authService.RevokePermissionFromRoleAsync(roleId, permissionId);
        if (!ok) return NotFound();
        return NoContent();
    }

    // ── Users write ──────────────────────────────────────────────────────────

    [HttpPatch("users/{userId:guid}/status")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> SetUserStatus(Guid userId, [FromBody] SetUserStatusDto dto)
    {
        var ok = await _authService.SetUserStatusAsync(userId, dto.IsActive);
        if (!ok) return NotFound();
        return NoContent();
    }
}
