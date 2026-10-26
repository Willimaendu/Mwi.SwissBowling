using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players;

public partial class PlayerStats
{
    private const int TrendLength = 30;
    private static readonly string[] PlacementLabels = ["Podest", "Auszeichnung", "ohne Auszeichnung"];
    private static readonly string[] Palette = ["#DC3545", "#01978C", "#DEE2E6"];

    private static readonly ChartOptions DonutOptions = new() { ChartPalette = Palette };

    [Parameter, EditorRequired] public IReadOnlyList<PlayerResultDto> Results { get; set; } = [];

    private int _totalPins;
    private int _totalThrows;
    private double? _average;
    private PlayerResultDto? _best;
    private int _podium;
    private int _awards;
    private double _awardRate;

    private List<PlayerResultDto> _trend = [];
    private ChartOptions _lineOptions = new();
    private ChartOptions _barOptions = new();
    private List<ChartSeries> _trendSeries = [];
    private string[] _trendLabels = [];
    private List<ChartSeries> _yearSeries = [];
    private string[] _yearLabels = [];
    private double[] _placementData = [];

    protected override void OnParametersSet()
    {
        _totalPins = Results.Sum(r => r.Pins);
        _totalThrows = Results.Sum(r => r.Throws);
        _average = _totalThrows == 0 ? null : (double)_totalPins / _totalThrows;
        _best = Results.Where(r => r.Throws > 0).MaxBy(r => r.Average);
        _podium = Results.Count(r => r.Rank is >= 1 and <= 3);
        _awards = Results.Count(r => r.Award);
        _awardRate = Results.Count == 0 ? 0 : (double)_awards / Results.Count;

        // Verlauf: chronologisch, auf die letzten Meisterschaften begrenzt; Beschriftung nur jede n-te, damit sie lesbar bleibt.
        _trend = Results.OrderBy(r => r.Date).TakeLast(TrendLength).ToList();
        var step = Math.Max(1, (int)Math.Ceiling(_trend.Count / 8.0));
        _trendLabels = _trend.Select((r, i) => i % step == 0 ? r.Date.ToString("MM.yy", CultureInfo.InvariantCulture) : string.Empty).ToArray();
        // Diagramme in «Holz auf 100 Wurf» (770 statt 7.70): die Zahl, in der Kegler denken – und MudChart kann die Achse
        // nur in ganzen Schritten teilen, mit 7.70 wäre keine feine Skala möglich.
        var trendValues = _trend.Select(r => Math.Round(r.Average * 100)).ToArray();
        _trendSeries = [new ChartSeries { Name = "Schnitt auf 100 Wurf", Data = trendValues }];
        _lineOptions = AxisOptions(trendValues);
        // Die Spline-Interpolation von MudChart braucht mindestens vier Punkte – darunter gerade Linien.
        _lineOptions.InterpolationOption = _trend.Count >= 4 ? InterpolationOption.NaturalSpline : InterpolationOption.Straight;
        _lineOptions.LineStrokeWidth = 2;

        var years = Results.GroupBy(r => r.Date.Year).OrderBy(g => g.Key)
            .Select(g => (Year: g.Key, Average: (double)g.Sum(r => r.Pins) / Math.Max(1, g.Sum(r => r.Throws)))).ToList();
        var yearValues = years.Select(y => Math.Round(y.Average * 100)).ToArray();
        _yearLabels = years.Select(y => y.Year.ToString(CultureInfo.InvariantCulture)).ToArray();
        _yearSeries = [new ChartSeries { Name = "Jahresschnitt auf 100 Wurf", Data = yearValues }];
        _barOptions = AxisOptions(yearValues);
        _barOptions.ChartPalette = [Palette[1]];

        _placementData = [_podium, Results.Count(r => r.Award && r.Rank > 3), Results.Count(r => !r.Award)];
    }

    /// <summary>
    /// Y-Achse dynamisch: beginnt beim tiefsten Wert statt bei null und teilt die Spannweite in etwa fünf «runde» Schritte
    /// (5, 10, 20, 25, 50 …) – so sind Unterschiede von wenigen Holz sichtbar.
    /// </summary>
    private static ChartOptions AxisOptions(double[] values)
    {
        var span = values.Length == 0 ? 0 : values.Max() - values.Min();
        var raw = Math.Max(span, 20) / 5;
        var step = NiceSteps.First(s => s >= raw);
        return new ChartOptions
        {
            YAxisTicks = step,
            MaxNumYAxisTicks = 8,
            YAxisRequireZeroPoint = false,
            YAxisFormat = "0",
            ChartPalette = Palette
        };
    }

    private static readonly int[] NiceSteps = [5, 10, 20, 25, 50, 100, 200, 500, 1000];

    private static string Format(double? value) => value is { } v ? v.ToString("0.00", CultureInfo.InvariantCulture) : "–";
}
