using System.Security.Claims;
using Ibr.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Ibr.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<AuthenticatedUserDto>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "El correo corporativo o la contraseña no son válidos." });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        if (user.EmployeeId is { } employeeId)
        {
            claims.Add(new Claim("employee_id", employeeId.ToString()));
        }

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Ok(user);
    }

    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return NoContent();
    }
}
