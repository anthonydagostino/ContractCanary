namespace OppSignal.Worker.Configuration;

public class DigestOptions
{
    public const string SectionName = "Digest";

    /// <summary>Local hour (0–23) at which to send each user's daily digest. Default 07:00.</summary>
    public int SendHourLocal { get; set; } = 7;
}
