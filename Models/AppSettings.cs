namespace OpenTuningTool.Models;

public enum TableViewMode
{
    Text = 0,
    TwoD = 1,
    ThreeD = 2,
}

public enum UiDensity
{
    Compact = 0,
    Comfortable = 1,
    Spacious = 2,
}

public enum AppTheme
{
    Dark = 0,
    Light = 1,
}

public sealed class AppSettings
{
    public float CalibrAiMinConfidence { get; set; } = 0.30f;

    public string CalibrAiBaseUrl { get; set; } = "http://localhost:8721";

    public AppTheme Theme { get; set; } = AppTheme.Dark;

    public TableViewMode DefaultTableViewMode { get; set; } = TableViewMode.Text;

    public UiDensity UiDensity { get; set; } = UiDensity.Comfortable;

    public bool AutoExpandTreeNodes { get; set; } = true;

    public bool PromptBeforeDiscardingBinChanges { get; set; } = true;

    public bool AutoLoadLastFilesOnStartup { get; set; } = false;

    public string? LastXdfPath { get; set; }

    public string? LastBinPath { get; set; }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            CalibrAiMinConfidence = CalibrAiMinConfidence,
            CalibrAiBaseUrl = CalibrAiBaseUrl,
            Theme = Theme,
            DefaultTableViewMode = DefaultTableViewMode,
            UiDensity = UiDensity,
            AutoExpandTreeNodes = AutoExpandTreeNodes,
            PromptBeforeDiscardingBinChanges = PromptBeforeDiscardingBinChanges,
            AutoLoadLastFilesOnStartup = AutoLoadLastFilesOnStartup,
            LastXdfPath = LastXdfPath,
            LastBinPath = LastBinPath,
        };
    }

    public void Normalize()
    {
        CalibrAiMinConfidence = Math.Clamp(CalibrAiMinConfidence, 0.0f, 1.0f);
        CalibrAiBaseUrl = NormalizeCalibrAiUrl(CalibrAiBaseUrl);

        if (!Enum.IsDefined(Theme))
            Theme = AppTheme.Dark;

        if (!Enum.IsDefined(DefaultTableViewMode))
            DefaultTableViewMode = TableViewMode.Text;

        if (!Enum.IsDefined(UiDensity))
            UiDensity = UiDensity.Comfortable;

        LastXdfPath = string.IsNullOrWhiteSpace(LastXdfPath) ? null : LastXdfPath.Trim();
        LastBinPath = string.IsNullOrWhiteSpace(LastBinPath) ? null : LastBinPath.Trim();
    }

    private static string NormalizeCalibrAiUrl(string? url)
    {
        string normalized = (url ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "http://localhost:8721";

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"http://{normalized}";
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
        {
            string clean = uri.ToString().TrimEnd('/');
            return clean;
        }

        return "http://localhost:8721";
    }
}
