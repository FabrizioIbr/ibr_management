using Ibr.Application.Portals;
using Ibr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Infrastructure.Portals;

/// <summary>
/// Usa un DbContext propio por consulta: el layout lo llama en cada navegación mientras la página
/// usa el DbContext del circuito, y un DbContext no admite operaciones simultáneas.
/// </summary>
public sealed class PortalService(IDbContextFactory<IbrDbContext> dbContextFactory) : IPortalService
{
    public async Task<IReadOnlyList<PortalDto>> GetPortalsAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var roleIds = await db.AppUsers
            .Where(user => user.UserId == userId && user.IsActive == true)
            .SelectMany(user => user.Roles.Select(role => role.RoleId))
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            try
            {
                roleIds = await db.Users
                    .Where(user => user.UserId == userId && user.DeletedAt == null && user.IsActive == true)
                    .SelectMany(user => user.Roles.Select(role => role.RoleId))
                    .ToListAsync(cancellationToken);
            }
            catch (System.Data.Common.DbException)
            {
                roleIds = [];
            }
        }

        var grantedPortalIds = await db.Portals
            .AsNoTracking()
            .Where(portal => portal.Users.Any(user => user.UserId == userId))
            .Select(portal => portal.PortalId)
            .ToListAsync(cancellationToken);

        var portals = await db.Portals
            .AsNoTracking()
            .Where(portal => portal.DeletedAt == null && portal.IsActive)
            .Select(portal => new
            {
                portal.PortalId,
                portal.Name,
                portal.Description,
                portal.ColorHex,
                portal.Icon,
                portal.Status,
                portal.RoleId,
                Sections = portal.Sections
                    .Where(section => section.IsActive)
                    .OrderBy(section => section.DisplayOrder).ThenBy(section => section.Title)
                    .Select(section => new { section.SectionId, section.Title, section.Description, section.Icon, section.Route, section.SectionGroup, section.Status })
                    .ToList()
            })
            .OrderBy(portal => portal.Name)
            .ToListAsync(cancellationToken);

        return portals.Select(portal =>
        {
            // Un portal nunca es público por omisión: requiere asignación directa
            // en UserPortals o el rol configurado en Portals.RoleID.
            var restricted = true;
            var hasAccess = grantedPortalIds.Contains(portal.PortalId)
                || portal.RoleId is { } roleId && roleIds.Contains(roleId);

            return new PortalDto(
                portal.PortalId,
                portal.Name,
                portal.Description,
                portal.ColorHex,
                portal.Icon,
                PortalStatus.Label(portal.Status, PortalStatus.PortalOptions),
                PortalStatus.Is(portal.Status, PortalStatus.Available),
                restricted,
                hasAccess && userId > 0,
                portal.Sections.Select(section => new PortalSectionDto(
                    section.SectionId,
                    section.Title,
                    section.Description,
                    section.Icon,
                    section.Route,
                    string.IsNullOrWhiteSpace(section.SectionGroup) ? null : section.SectionGroup.Trim(),
                    PortalStatus.Is(section.Status, PortalStatus.SectionActive))).ToList());
        }).ToList();
    }
}
