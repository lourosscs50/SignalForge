using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence.Conversions;

internal static class DecisionOptionsValueConverter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<IReadOnlyList<DecisionOptionSnapshot>?, string?> Instance = new(
        v => Serialize(v),
        v => Deserialize(v));

    private static string? Serialize(IReadOnlyList<DecisionOptionSnapshot>? v)
    {
        if (v is null || v.Count == 0)
            return null;
        return JsonSerializer.Serialize(v.ToList(), Json);
    }

    private static IReadOnlyList<DecisionOptionSnapshot>? Deserialize(string? v)
    {
        if (string.IsNullOrWhiteSpace(v))
            return null;
        var list = JsonSerializer.Deserialize<List<DecisionOptionSnapshot>>(v, Json);
        return list is null or { Count: 0 } ? null : list;
    }
}
