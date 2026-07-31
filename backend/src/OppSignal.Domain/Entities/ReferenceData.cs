namespace OppSignal.Domain.Entities;

/// <summary>Official NAICS code (2022). Seeded from the full Census file (levels 2–6).</summary>
public class NaicsCode
{
    public string Code { get; set; } = default!;   // 2–6 digit
    public string Title { get; set; } = default!;
    public int Level { get; set; }                   // digit length (2..6)
    public string? ParentCode { get; set; }
}

/// <summary>Product &amp; Service Code (curated seed — see DECISIONS.md).</summary>
public class PscCode
{
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Category { get; set; } = default!; // top-level grouping
    public bool IsService { get; set; }
}

/// <summary>Federal department / sub-tier reference for the agency typeahead.</summary>
public class Agency
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Tier { get; set; }                    // 1 = department, 2 = sub-tier
    public string? ParentCode { get; set; }
}

/// <summary>Set-aside program reference (code, name, description).</summary>
public class SetAsideRef
{
    public string Code { get; set; } = default!;     // e.g. "SDVOSBC"
    public string Name { get; set; } = default!;     // short label
    public string Description { get; set; } = default!;
}
