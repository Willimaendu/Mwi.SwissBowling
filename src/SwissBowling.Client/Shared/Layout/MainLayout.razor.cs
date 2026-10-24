namespace SwissBowling.Client.Shared.Layout;

public partial class MainLayout
{
    private static readonly (string Href, string Text)[] Links =
    [
        ("/", "Start"),
        ("/meisterschaften", "Meisterschaften"),
        ("/kegler", "Kegler"),
        ("/klubs", "Klubs"),
        ("/unterverbaende", "Unterverbände")
    ];

    private bool _drawerOpen;
    private bool _darkMode;
}
