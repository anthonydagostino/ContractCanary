using OppSignal.Application.Abstractions;

namespace OppSignal.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
