using MudBlazor;

namespace SwissBowling.Client;

/// <summary>Theme angelehnt an www.sfkv.ch: Arial/Helvetica, Grautöne für Text, SFKV-Rot als Akzent, Petrol als Zweitfarbe.</summary>
public static class AppTheme
{
    public const string Logo = "images/sfkv-logo.png";

    /// <summary>Höhe virtualisierter Tabellen: Viewport abzüglich App-Bar, Seitenkopf und Toolbar.</summary>
    public const string TableHeight = "calc(100vh - 300px)";

    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#DC3545",
            PrimaryDarken = "#BB2D3B",
            Secondary = "#01978C",
            Tertiary = "#014684",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#555555",
            Background = "#F8F8F8",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#555555",
            TextPrimary = "#555555",
            TextSecondary = "#777777",
            LinesDefault = "#DEE2E6",
            TableLines = "#DEE2E6",
            Divider = "#DEE2E6",
            Success = "#198754",
            Warning = "#C9A227",
            Error = "#BB2D3B",
            Info = "#01978C"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#E4606D",
            Secondary = "#2CB5AA",
            Tertiary = "#5B8FD6",
            AppbarBackground = "#1F2124",
            Background = "#141517",
            Surface = "#1F2124",
            DrawerBackground = "#1F2124",
            TextPrimary = "#E6E6E6",
            TextSecondary = "#A0A0A0",
            LinesDefault = "#34373B",
            TableLines = "#34373B",
            Divider = "#34373B"
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "6px", AppbarHeight = "72px" },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Arial", "Helvetica", "sans-serif"], FontSize = "1rem", LineHeight = "1.5" },
            H3 = new H3Typography { FontFamily = ["Arial", "Helvetica", "sans-serif"], FontSize = "2.25rem", FontWeight = "500", LineHeight = "1.2" },
            H4 = new H4Typography { FontFamily = ["Arial", "Helvetica", "sans-serif"], FontSize = "1.75rem", FontWeight = "500", LineHeight = "1.25" },
            H5 = new H5Typography { FontFamily = ["Arial", "Helvetica", "sans-serif"], FontSize = "1.4rem", FontWeight = "600" },
            H6 = new H6Typography { FontFamily = ["Arial", "Helvetica", "sans-serif"], FontSize = "1.15rem", FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontFamily = ["Arial", "Helvetica", "sans-serif"], FontWeight = "600" },
            Button = new ButtonTypography { FontFamily = ["Arial", "Helvetica", "sans-serif"], FontWeight = "500", TextTransform = "none" }
        }
    };
}
