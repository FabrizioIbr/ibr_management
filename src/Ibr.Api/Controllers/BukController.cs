using Ibr.Application.Buk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/buk")]
public sealed class BukController(IBukSyncService bukSync) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<ActionResult<BukSyncResult>> Sync(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await bukSync.SyncAsync(null, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Problem(title: "No se pudo sincronizar Buk", detail: exception.Message);
        }
    }
}
