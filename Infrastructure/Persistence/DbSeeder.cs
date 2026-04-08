using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ComponentesIA.Infrastructure.Persistence;

public static class DbSeeder
{
    // ── Permission name constants ─────────────────────────────────────────
    private const string PViewBatches   = "view_batches";
    private const string PEditTemplates = "edit_templates";
    private const string PManageUsers   = "manage_users";
    private const string PViewResults   = "view_results";
    private const string PValidate      = "validate";
    private const string PUpload        = "upload_document";

    public static async Task SeedAsync(AppDbContext db)
    {
        // ── Permissions: upsert by Name to avoid duplicate key errors ─────
        var permDefs = new[]
        {
            (Name: PViewBatches,   Desc: "Ver lotes",             Cat: "batches"),
            (Name: PEditTemplates, Desc: "Gestionar plantillas",  Cat: "templates"),
            (Name: PManageUsers,   Desc: "Gestionar usuarios",    Cat: "users"),
            (Name: PViewResults,   Desc: "Ver resultados",        Cat: "results"),
            (Name: PValidate,      Desc: "Validar extracciones",  Cat: "results"),
            (Name: PUpload,        Desc: "Subir documentos",      Cat: "batches"),
        };

        foreach (var def in permDefs)
        {
            var existing = await db.Permissions.FirstOrDefaultAsync(p => p.Name == def.Name);
            if (existing == null)
                db.Permissions.Add(new Permission { Id = Guid.NewGuid(), Name = def.Name, Description = def.Desc, Category = def.Cat });
            else
            {
                existing.Description = def.Desc;
                existing.Category    = def.Cat;
            }
        }
        await db.SaveChangesAsync();

        // Resolve actual permission IDs from DB
        var permNames = permDefs.Select(d => d.Name).ToArray();
        var permMap   = await db.Permissions
            .Where(p => permNames.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, p => p.Id);

        // ── Roles: upsert by Name, resolve actual IDs ─────────────────────
        var roleDefs = new[]
        {
            (Name: "Admin",   Desc: "Administrador del sistema"),
            (Name: "Analyst", Desc: "Analista de extracción"),
            (Name: "Viewer",  Desc: "Solo lectura"),
        };

        foreach (var def in roleDefs)
        {
            var existing = await db.Roles.FirstOrDefaultAsync(r => r.Name == def.Name);
            if (existing == null)
                db.Roles.Add(new Role { Id = Guid.NewGuid(), Name = def.Name, Description = def.Desc });
            else
                existing.Description = def.Desc;
        }
        await db.SaveChangesAsync();

        // Resolve actual role IDs from DB
        var roleNames = roleDefs.Select(d => d.Name).ToArray();
        var roleMap   = await db.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name, r => r.Id);

        // ── Role → Permission assignments ─────────────────────────────────
        var adminPerms   = new[] { PViewBatches, PEditTemplates, PManageUsers, PViewResults, PValidate, PUpload };
        var analystPerms = new[] { PViewBatches, PViewResults, PValidate, PUpload };
        var viewerPerms  = new[] { PViewBatches, PViewResults };

        await SeedRolePermissions(db, roleMap["Admin"],   adminPerms,   permMap);
        await SeedRolePermissions(db, roleMap["Analyst"], analystPerms, permMap);
        await SeedRolePermissions(db, roleMap["Viewer"],  viewerPerms,  permMap);
        await db.SaveChangesAsync();

        // ── Admin user ────────────────────────────────────────────────────
        const string adminEmail = "admin@demo.com";
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id           = Guid.NewGuid(),
                UserName     = "admin",
                Email        = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
        }

        var adminRoleId = roleMap["Admin"];
        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRoleId))
        {
            db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRoleId });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedRolePermissions(
        AppDbContext db, Guid roleId, string[] permNames, Dictionary<string, Guid> permMap)
    {
        foreach (var name in permNames)
        {
            if (!permMap.TryGetValue(name, out var permId)) continue;
            if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permId))
                db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
        }
    }
}
