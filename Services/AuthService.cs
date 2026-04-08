using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace ComponentesIA.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email))
            throw new Exception("Usuario o email ya existe");

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserNameOrEmail || u.Email == request.UserNameOrEmail);
        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Credenciales inválidas");
        return await GenerateAuthResponse(user);
    }

    public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId) || !await _db.Roles.AnyAsync(r => r.Id == roleId))
            return false;
        if (await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId))
            return true;
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == roleId) || !await _db.Permissions.AnyAsync(p => p.Id == permissionId))
            return false;
        if (await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
            return true;
        _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetUserStatusAsync(Guid userId, bool isActive)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        user.IsActive = isActive;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<List<UserListItemDto>> GetUsersAsync()
    {
        return await _db.Users
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        return await _db.Roles
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync()
    {
        return await _db.Permissions
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category
            })
            .ToListAsync();
    }

    // ── Write (roles) ────────────────────────────────────────────────────────

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == dto.Name))
            throw new Exception($"Ya existe un rol con el nombre '{dto.Name}'");

        var role = new Role { Id = Guid.NewGuid(), Name = dto.Name, Description = dto.Description };
        _db.Roles.Add(role);

        foreach (var permId in dto.PermissionIds)
        {
            if (await _db.Permissions.AnyAsync(p => p.Id == permId))
                _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });
        }

        await _db.SaveChangesAsync();

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            PermissionIds = dto.PermissionIds
        };
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId);
        if (role == null) return false;
        _db.RolePermissions.RemoveRange(role.RolePermissions);
        _db.UserRoles.RemoveRange(role.UserRoles);
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rp = await _db.RolePermissions
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);
        if (rp == null) return false;
        _db.RolePermissions.Remove(rp);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<AuthResponseDto> GenerateAuthResponse(User user)
    {
        var roles = await _db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name).ToListAsync();
        var permissions = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name)).Distinct().ToListAsync();
        var token = GenerateJwtToken(user, roles, permissions);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles,
            Permissions = permissions
        };
    }

    private string GenerateJwtToken(User user, List<string> roles, List<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
