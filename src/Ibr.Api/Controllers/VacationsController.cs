using Ibr.Application.Vacations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vacations")]
public sealed class VacationsController(IVacationAdminService vacations) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<ActionResult<VacationKpisDto>> Kpis(CancellationToken cancellationToken) =>
        Ok(await vacations.GetKpisAsync(cancellationToken));

    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<VacationRequestDto>>> Requests(
        [FromQuery] string? employee,
        [FromQuery] DateOnly? startFrom,
        [FromQuery] string? status,
        CancellationToken cancellationToken) =>
        Ok(await vacations.SearchRequestsAsync(new VacationRequestFilterDto(employee, startFrom, status), cancellationToken));

    [HttpGet("employees")]
    public async Task<ActionResult<IReadOnlyList<EmployeeSearchDto>>> Employees(
        [FromQuery] string? term,
        CancellationToken cancellationToken) =>
        Ok(await vacations.SearchEmployeesAsync(term ?? string.Empty, cancellationToken));

    [HttpGet("employees/{employeeId:int}")]
    public async Task<ActionResult<EmployeeVacationDto>> EmployeeVacations(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var result = await vacations.GetEmployeeVacationsAsync(employeeId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
