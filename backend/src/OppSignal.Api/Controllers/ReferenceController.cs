using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Application.Reference;

namespace OppSignal.Api.Controllers;

[ApiController]
[Route("api/reference")]
[Authorize]
public sealed class ReferenceController : ControllerBase
{
    private readonly IReferenceService _reference;

    public ReferenceController(IReferenceService reference) => _reference = reference;

    [HttpGet("naics")]
    public async Task<ActionResult<IReadOnlyList<NaicsDto>>> Naics([FromQuery] string? q, [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await _reference.SearchNaicsAsync(q, limit, ct));

    [HttpGet("psc")]
    public async Task<ActionResult<IReadOnlyList<PscDto>>> Psc([FromQuery] string? q, [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await _reference.SearchPscAsync(q, limit, ct));

    [HttpGet("agencies")]
    public async Task<ActionResult<IReadOnlyList<AgencyDto>>> Agencies([FromQuery] string? q, [FromQuery] int limit = 40, CancellationToken ct = default)
        => Ok(await _reference.SearchAgenciesAsync(q, limit, ct));

    [HttpGet("set-asides")]
    public async Task<ActionResult<IReadOnlyList<SetAsideDto>>> SetAsides(CancellationToken ct)
        => Ok(await _reference.GetSetAsidesAsync(ct));

    [HttpGet("notice-types")]
    public ActionResult<IReadOnlyList<NoticeTypeDto>> NoticeTypes()
        => Ok(_reference.GetNoticeTypes());
}
