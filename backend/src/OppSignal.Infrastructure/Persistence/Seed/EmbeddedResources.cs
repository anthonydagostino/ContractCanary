using System.Reflection;

namespace OppSignal.Infrastructure.Persistence.Seed;

internal static class EmbeddedResources
{
    private static readonly Assembly Asm = typeof(EmbeddedResources).Assembly;

    public static string ReadText(string shortName)
    {
        // shortName like "naics.json" -> "OppSignal.Infrastructure.Persistence.Seed.naics.json"
        var full = Asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("." + shortName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded resource '{shortName}' not found.");
        using var stream = Asm.GetManifestResourceStream(full)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
