using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Api.Infrastructure;
using OppSignal.Application.Profiles;

namespace OppSignal.Api.Controllers;

[ApiController]
[Route("api/profiles")]
[Authorize]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileService _profiles;

    public ProfilesController(IProfileService profiles) => _profiles = profiles;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MatchProfileDto>>> List(CancellationToken ct)
        => Ok(await _profiles.ListAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MatchProfileDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _profiles.GetAsync(User.GetUserId(), id, ct));

    [HttpPost]
    public async Task<ActionResult<MatchProfileDto>> Create(
        [FromBody] ProfileInput input, [FromServices] IValidator<ProfileInput> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(input, ct);
        var created = await _profiles.CreateAsync(User.GetUserId(), input, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MatchProfileDto>> Update(
        Guid id, [FromBody] ProfileInput input, [FromServices] IValidator<ProfileInput> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(input, ct);
        return Ok(await _profiles.UpdateAsync(User.GetUserId(), id, input, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _profiles.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
