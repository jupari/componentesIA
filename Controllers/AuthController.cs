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
}
