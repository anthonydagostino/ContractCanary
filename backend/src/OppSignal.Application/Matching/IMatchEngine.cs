using OppSignal.Domain.Entities;

namespace OppSignal.Application.Matching;

public interface IMatchEngine
{
    MatchOutcome Evaluate(Notice notice, MatchProfile profile);
}
