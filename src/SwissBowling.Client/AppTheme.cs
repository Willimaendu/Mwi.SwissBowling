using MudBlazor;

namespace SwissBowling.Client;

/// <summary>Zentrales MudBlazor-Theme (hell/dunkel).</summary>
public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#B3261E",
            Secondary = "#1F2937",
            Tertiary = "#0F766E",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#111827",
            Background = "#F6F7F9",
            DrawerBackground = "#FFFFFF"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#F87171",
            Secondary = "#CBD5E1",
            Tertiary = "#2DD4BF",
            AppbarBackground = "#111827",
            Background = "#0B1220",
            Surface = "#111827",
            DrawerBackground = "#111827"
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "10px" },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Inter", "Segoe UI", "Roboto", "sans-serif"] }
        }
    };
}
