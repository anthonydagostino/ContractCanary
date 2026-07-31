using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OppSignal.Infrastructure.Persistence;

/// <summary>
/// Converters + comparers for mapping <c>List&lt;TEnum&gt;</c> profile filters to
/// Postgres <c>integer[]</c> columns. (String lists map to <c>text[]</c> natively.)
/// </summary>
internal static class Converters
{
    public static ValueConverter<List<TEnum>, int[]> EnumListToIntArray<TEnum>()
        where TEnum : struct, Enum
        => new(
            v => v.Select(e => Convert.ToInt32(e)).ToArray(),
            v => v.Select(i => (TEnum)Enum.ToObject(typeof(TEnum), i)).ToList());

    public static ValueComparer<List<TEnum>> EnumListComparer<TEnum>()
        where TEnum : struct, Enum
        => new(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
            v => v.ToList());

    public static ValueComparer<List<string>> StringListComparer()
        => new(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e == null ? 0 : e.GetHashCode())),
            v => v.ToList());
}
