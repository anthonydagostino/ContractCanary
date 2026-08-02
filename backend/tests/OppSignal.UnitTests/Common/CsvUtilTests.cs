using FluentAssertions;
using OppSignal.Application.Common;
using Xunit;

namespace OppSignal.UnitTests.Common;

public class CsvUtilTests
{
    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("-2+3", "'-2+3")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\ttab", "'\ttab")]
    public void Neutralizes_formula_injection_leading_characters(string input, string expectedInner)
        => CsvUtil.Cell(input).Should().Be(expectedInner);

    [Fact]
    public void Formula_value_that_also_needs_quoting_is_prefixed_then_quoted()
        // '=HYPERLINK(...) contains a comma → apostrophe first, then RFC4180 quoting.
        => CsvUtil.Cell("=HYPERLINK(\"x\",\"y\")")
            .Should().Be("\"'=HYPERLINK(\"\"x\"\",\"\"y\"\")\"");

    [Fact]
    public void Plain_value_is_unquoted()
        => CsvUtil.Cell("IT Support Services").Should().Be("IT Support Services");

    [Theory]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("a\"b", "\"a\"\"b\"")]
    [InlineData("line1\nline2", "\"line1\nline2\"")]
    public void Quotes_and_escapes_special_characters(string input, string expected)
        => CsvUtil.Cell(input).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_or_null_is_empty(string? input)
        => CsvUtil.Cell(input).Should().Be("");
}
