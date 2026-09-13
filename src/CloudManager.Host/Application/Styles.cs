namespace CloudManager.Host.Application;

using MudBlazor;

// Oracle Red based palette to tell the OCI console apart from the AWS one
public static class Styles
{
    private const string OracleRed = "#C74634";
    private const string OracleRedDark = "#A63A2B";
    private const string OracleRedLight = "#DB6A58";
    private const string OracleAccent = "#F0862B";
    private const string OracleSlate = "#312D2A";

    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = OracleRed,
            PrimaryDarken = OracleRedDark,
            PrimaryLighten = OracleRedLight,
            Secondary = OracleAccent,
            AppbarBackground = OracleSlate,
            AppbarText = Colors.Shades.White,
            DrawerBackground = Colors.Gray.Lighten4,
            DrawerText = Colors.Gray.Darken4,
            Background = Colors.Gray.Lighten5,
            Surface = Colors.Shades.White
        },
        PaletteDark = new PaletteDark()
    };

    public static DialogOptions MediumDialog { get; } = new() { MaxWidth = MaxWidth.Medium, FullWidth = true };

    public static DialogOptions LargeDialog { get; } = new() { MaxWidth = MaxWidth.Large, FullWidth = true };
}
