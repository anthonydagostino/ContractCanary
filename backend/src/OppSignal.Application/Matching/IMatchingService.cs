using OppSignal.Domain.Entities;

namespace OppSignal.Application.Matching;

public interface IMatchingService
{
    Task<int> MatchNoticesAsync(IReadOnlyCollection<Notice> notices, CancellationToken ct = default);

    /// <summary>Rebuild a profile's matches. Scoped by <paramref name="userId"/> so a
    /// profile id from one tenant can never rebuild/delete another tenant's matches.</summary>
    Task<int> BackfillProfileAsync(Guid profileId, Guid userId, CancellationToken ct = default);
}
