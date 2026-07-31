using OppSignal.Application.Abstractions;

namespace OppSignal.IntegrationTests.Support;

public sealed class FixedClock : IClock
{
    public FixedClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; set; }
}
