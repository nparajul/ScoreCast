namespace ScoreCast.Web.Theme;

public static class AppTheme
{
    private static readonly PaletteLight LightPalette = new()
    {
        Primary = "#0A1929",
        Secondary = "#37003C",
        Tertiary = "#FF6B35",
        AppbarBackground = "#0A1929",
        AppbarText = "#FFFFFF",
        Background = "#F5F7FA",
        Surface = "#FFFFFF",
        DrawerBackground = "#0A1929",
        DrawerText = "#FFFFFF",
        TextSecondary = "#555555"
    };

    public static readonly MudTheme Theme = new()
    {
        PaletteLight = LightPalette,
        PaletteDark = new PaletteDark
        {
            Primary = LightPalette.Primary,
            Secondary = LightPalette.Secondary,
            Tertiary = LightPalette.Tertiary,
            AppbarBackground = LightPalette.AppbarBackground,
            AppbarText = LightPalette.AppbarText,
            Background = LightPalette.Background,
            Surface = LightPalette.Surface,
            DrawerBackground = LightPalette.DrawerBackground,
            DrawerText = LightPalette.DrawerText,
            TextSecondary = LightPalette.TextSecondary,
            TextPrimary = "#333333",
            ActionDefault = "#555555",
            LinesDefault = "#E0E0E0"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "system-ui", "-apple-system", "sans-serif"],
                LetterSpacing = "-0.011em"
            },
            H5 = new H5Typography { FontWeight = "800", LetterSpacing = "-0.02em" },
            H6 = new H6Typography { FontWeight = "700", LetterSpacing = "-0.02em" },
            Subtitle1 = new Subtitle1Typography { FontWeight = "600" },
            Subtitle2 = new Subtitle2Typography { FontWeight = "600" },
            Body1 = new Body1Typography { LetterSpacing = "-0.011em" },
            Body2 = new Body2Typography { LetterSpacing = "-0.006em" },
            Button = new ButtonTypography { FontWeight = "700", LetterSpacing = "0.01em" }
        }
    };
}
