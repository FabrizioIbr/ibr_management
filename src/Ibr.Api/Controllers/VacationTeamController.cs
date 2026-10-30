using System.Security.Claims;
using Ibr.Application.Vacations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vacaciones")]
public sealed class VacationTeamController(IVacationTeamService team) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<VacationDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        var employeeId = EmployeeId();
        if (employeeId <= 0) return Ok(new VacationDashboardDto(0, 0, 0, 0, new Dictionary<string, int>(), []));
        return Ok(await team.GetDashboardAsync(employeeId, unrestricted: false, cancellationToken));
    }

    [HttpGet("saldos")]
    public async Task<ActionResult<IReadOnlyList<VacationBalanceDto>>> Saldos(CancellationToken cancellationToken)
    {
        var employeeId = EmployeeId();
        if (employeeId <= 0) return Ok(Array.Empty<VacationBalanceDto>());
        return Ok(await team.GetBalancesAsync(employeeId, unrestricted: false, cancellationToken));
    }

    [HttpGet("solicitudes")]
    public async Task<ActionResult<IReadOnlyList<VacationRequestDto>>> Solicitudes(
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? employee,
        CancellationToken cancellationToken)
    {
        var employeeId = EmployeeId();
        if (employeeId <= 0) return Ok(Array.Empty<VacationRequestDto>());
        return Ok(await team.GetTeamRequestsAsync(employeeId, unrestricted: false, status, from, to, employee, cancellationToken));
    }

    [HttpGet("periodos/{employeeId:int}")]
    public async Task<ActionResult<EmployeeVacationDto>> Periodos(int employeeId, CancellationToken cancellationToken)
    {
        var bossEmployeeId = EmployeeId();
        if (bossEmployeeId <= 0) return Forbid();
        var result = await team.GetMemberPeriodsAsync(bossEmployeeId, employeeId, unrestricted: false, cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("colaborador/{employeeId:int}/resumen")]
    public async Task<ActionResult<VacationColaboradorResumenDto>> ColaboradorResumen(int employeeId, CancellationToken cancellationToken)
    {
        var bossEmployeeId = EmployeeId();
        if (bossEmployeeId <= 0) return Forbid();
        var result = await team.GetColaboradorResumenAsync(bossEmployeeId, employeeId, unrestricted: false, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private int EmployeeId() =>
        int.TryParse(User.FindFirstValue("employee_id"), out var id) ? id : 0;
}
