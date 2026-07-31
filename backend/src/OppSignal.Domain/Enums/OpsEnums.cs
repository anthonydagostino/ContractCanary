namespace OppSignal.Domain.Enums;

/// <summary>Which upstream an ingest run pulled from.</summary>
public enum IngestSource
{
    Fixture = 0,
    Sam = 1,
}

/// <summary>Lifecycle of a single ingest run (audited in <c>IngestRun</c>).</summary>
public enum IngestStatus
{
    Running = 0,
    Succeeded = 1,
    Failed = 2,
}

/// <summary>Category of a sent email (audited in <c>EmailLog</c>).</summary>
public enum EmailKind
{
    Verification = 0,
    PasswordReset = 1,
    Digest = 2,
    Welcome = 3,
}
