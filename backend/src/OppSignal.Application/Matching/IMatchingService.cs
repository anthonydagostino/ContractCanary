using OppSignal.Domain.Entities;

namespace OppSignal.Application.Matching;

public interface IMatchingService
{
    Task<int> MatchNoticesAsync(IReadOnlyCollection<Notice> notices, CancellationToken ct = default);
    Task<int> BackfillProfileAsync(Guid profileId, CancellationToken ct = default);
}
