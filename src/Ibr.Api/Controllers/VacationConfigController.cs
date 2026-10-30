using Ibr.Application.Vacations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibr.Api.Controllers;

public sealed record VacationSettingsRequest(int AlertDays, int CriticalDays);

[ApiController]
[Authorize(Roles = "System_Admin,RRHH")]
[Route("api/configuracion/vacaciones")]
public sealed class VacationConfigController(IVacationSettingsService settings) : ControllerBase
{
    [HttpGet]
    public ActionResult<VacationSettingsDto> Get() => Ok(settings.Current);

    [HttpPut]
    public async Task<ActionResult<VacationSettingsDto>> Put(VacationSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await settings.SaveAsync(request.AlertDays, request.CriticalDays, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Problem(title: "Configuración inválida", detail: exception.Message);
        }
    }
}
