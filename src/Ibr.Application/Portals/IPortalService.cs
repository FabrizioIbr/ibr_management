namespace Ibr.Application.Portals;

public interface IPortalService
{
    /// <summary>
    /// Portales activos con sus secciones visibles, ordenadas. <see cref="PortalDto.HasAccess"/> indica si el usuario puede entrar:
    /// el portal no tiene restricción, el usuario tiene el rol del portal (Portals.RoleID) o está asignado en UserPortals.
    /// </summary>
    Task<IReadOnlyList<PortalDto>> GetPortalsAsync(int userId, CancellationToken cancellationToken = default);
}
