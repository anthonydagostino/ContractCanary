using FluentAssertions;
using OppSignal.Application.Ingest;
using OppSignal.Domain.Entities;
using Xunit;

namespace OppSignal.UnitTests.Ingest;

public class IngestCopyMutableTests
{
    private static Notice Base(string? description) => new()
    {
        NoticeId = "N1",
        Title = "Original title",
        Description = description,
        RawJson = "{}",
    };

    [Fact]
    public void Update_copies_a_changed_description()
    {
        // Regression: CopyMutable skipped Description entirely, so a
        // description-only amendment updated RawJson but left the stored text —
        // which drives keyword matching and the detail page — stale forever.
        var target = Base("old text");
        var src = Base("new text after amendment");

        IngestService.CopyMutable(target, src);

        target.Description.Should().Be("new text after amendment");
    }

    [Fact]
    public void Update_does_not_clobber_stored_description_with_null()
    {
        // The live list API carries only a description link; the resolved text
        // arrives separately. An update without resolved text must keep it.
        var target = Base("resolved text we already have");
        var src = Base(null);

        IngestService.CopyMutable(target, src);

        target.Description.Should().Be("resolved text we already have");
    }

    [Fact]
    public void Update_copies_the_ordinary_mutable_fields()
    {
        var target = Base(null);
        var src = Base(null);
        src.Title = "Amended title";
        src.NaicsCode = "541511";
        src.IsActive = false;

        IngestService.CopyMutable(target, src);

        target.Title.Should().Be("Amended title");
        target.NaicsCode.Should().Be("541511");
        target.IsActive.Should().BeFalse();
    }
}
