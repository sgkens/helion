namespace Helion.Host.Gateway;

using Spectre.Console;

public static class HelionPalette
{
    // Symbols mandated by GEMINI.md
    public const string SymbolBlue = "🟦";
    public const string SymbolGreen = "🟩";
    public const string SymbolRed = "🟥";
    public const string SymbolOrange = "🔸";
    public const string SymbolLightBlue = "🔹";
    public const string SymbolDiamond = "💠";
    public const string SymbolRefresh = "🔄";

    // Palette Colors
    public static readonly Color Primary = Color.Blue;
    public static readonly Color Success = Color.Green;
    public static readonly Color Error = Color.Red;
    public static readonly Color Warning = Color.Orange1;
    public static readonly Color Accent = Color.DeepSkyBlue1;
    public static readonly Color Subtle = Color.Grey;

    public static Style StylePrimary => new Style(Primary);
    public static Style StyleAccent => new Style(Accent);
}
