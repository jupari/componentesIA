using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ComponentesIA.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Permisos básicos
        var permisos = new[]
        {
            new Permission { Id = Guid.NewGuid(), Name = "upload_document", Description = "Subir documentos" },
            new Permission { Id = Guid.NewGuid(), Name = "manage_users", Description = "Gestionar usuarios" },
            new Permission { Id = Guid.NewGuid(), Name = "manage_templates", Description = "Gestionar plantillas" }
        };
        foreach (var p in permisos)
        {
            if (!await db.Permissions.AnyAsync(x => x.Name == p.Name))
                db.Permissions.Add(p);
        }

        // Guardar permisos antes de crear RolePermission
        await db.SaveChangesAsync();

        // Rol admin
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "Administrador" };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

        // Asignar todos los permisos al rol admin
        foreach (var p in await db.Permissions.ToListAsync())
        {
            if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == p.Id))
                db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id });
        }

        // Usuario admin
        var adminEmail = "admin@demo.com";
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(adminUser);
        }
        // Asignar rol admin al usuario admin
        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id))
            db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

        await db.SaveChangesAsync();
    }
}
