namespace OppSignal.Domain.Enums;

/// <summary>Where a saved opportunity sits in the user's pursuit pipeline.</summary>
public enum PipelineStatus
{
    Reviewing = 0,
    Pursuing = 1,
    Submitted = 2,
    Won = 3,
    Lost = 4,
    Passed = 5,
}
