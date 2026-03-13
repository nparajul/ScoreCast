namespace ScoreCast.Web.Theme;

public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0A1929",
            Secondary = "#37003C",
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
            Primary = "#8B1A9E",
            Secondary = "#FF6B35",
            Tertiary = "#8B1A9E",
            Surface = "#132F4C",
            Background = "#0A1929",
            AppbarBackground = "#0A1929",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#071318",
            DrawerText = "#FFFFFF",
            TextPrimary = "#E0E0E0",
            TextSecondary = "#B0BEC5"
        }
    };

}
