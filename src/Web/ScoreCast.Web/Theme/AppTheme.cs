namespace ScoreCast.Web.Theme;

public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0A1929",
            Secondary = "#00E5FF",
            Tertiary = "#FF6B35",
            AppbarBackground = "#0A1929",
            AppbarText = "#FFFFFF",
            Background = "#F5F7FA",
            Surface = "#FFFFFF",
            DrawerBackground = "#0A1929",
            DrawerText = "#FFFFFF"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#00E5FF",
            Secondary = "#FF6B35",
            Tertiary = "#00E5FF",
            Surface = "#132F4C",
            Background = "#0A1929",
            AppbarBackground = "#0A1929",
            AppbarText = "#00E5FF",
            DrawerBackground = "#071318",
            DrawerText = "#00E5FF"
        }
    };

}
