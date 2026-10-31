using System.Security.Claims;
using Ibr.Application.Vacations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/planificacion")]
public sealed class PlanificacionController(IVacationTeamService team) : ControllerBase
{
    /// <summary>Matriz de planificación (empleado × próximos 4 meses) del equipo del jefe autenticado.</summary>
    [HttpGet("matriz")]
    public async Task<ActionResult<VacationMatrizDto>> Matriz(CancellationToken cancellationToken)
    {
        var employeeId = EmployeeId();
        if (employeeId <= 0) return Ok(new VacationMatrizDto([], []));
        return Ok(await team.GetMatrizAsync(employeeId, unrestricted: false, cancellationToken));
    }

    private int EmployeeId() =>
        int.TryParse(User.FindFirstValue("employee_id"), out var id) ? id : 0;
}
