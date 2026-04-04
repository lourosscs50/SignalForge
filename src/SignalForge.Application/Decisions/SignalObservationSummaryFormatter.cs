using SignalForge.Domain;

namespace SignalForge.Application.Decisions;

internal static class SignalObservationSummaryFormatter
{
    public static string Format(Signal signal)
    {
        var source = TrimLimit(signal.Source, 128);
        var type = TrimLimit(signal.Type, 128);
        var value = signal.Value is null ? "null" : signal.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"signalId={signal.Id};source={source};type={type};value={value}";
    }

    private static string TrimLimit(string value, int max)
    {
        var v = (value ?? string.Empty).Trim();
        return v.Length <= max ? v : v[..max];
    }
}
