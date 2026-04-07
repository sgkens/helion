namespace Helion.Host.Gateway;

using Spectre.Console;
using System;

public static class UI
{
    private const string LogoTemplate = @"
      :::    ::: :::::::::: :::        :::::::::::  ::::::::  ::::    ::: 
     :+:    :+: :+:        :+:            :+:     :+:    :+: :+:+:   :+:  
    +:+    +:+ +:+        +:+            +:+     +:+    +:+ :+:+:+  +:+   
   +#++:++#++ +#++:++#   +#+            +#+     +#+    +#+ +#+ +:+ +#+    
  +#+    +#+ +#+        +#+            +#+     +#+    +#+ +#+  +#+#+#     
 #+#    #+# #+#        #+#            #+#     #+#    #+# #+#   #+#+#      
###    ### ########## ########## ###########  ########  ###    ####       
                                                                          
                          {SYMBOL} HELION EDGE {VERSION}
";

    public static void PrintLogo(string version)
    {
        // 20-Pass Fix: Implement robust ASCII logo rendering
        // Using explicit [string] casting spirit for replacement as per memory fact
        string logo = ((string)LogoTemplate)
            .Replace("{SYMBOL}", HelionPalette.SymbolDiamond)
            .Replace("{VERSION}", $"v{version}");

        AnsiConsole.Write(new FigletText("HELION").Color(HelionPalette.Primary));
        AnsiConsole.MarkupLine($"[bold {HelionPalette.Accent.ToMarkup()}]" + Markup.Escape(logo) + "[/]");
        AnsiConsole.WriteLine();
    }

    public static void PrintHeader(string text)
    {
        var rule = new Rule($"[bold]{HelionPalette.SymbolBlue} {text.ToUpper()}[/]");
        rule.Justification = Justify.Left;
        rule.Style = HelionPalette.StylePrimary;
        AnsiConsole.Write(rule);
    }

    public static void Success(string message) => 
        AnsiConsole.MarkupLine($"[{HelionPalette.Success.ToMarkup()}]{HelionPalette.SymbolGreen} {Markup.Escape(message)}[/]");

    public static void Error(string message) => 
        AnsiConsole.MarkupLine($"[{HelionPalette.Error.ToMarkup()}]{HelionPalette.SymbolRed} {Markup.Escape(message)}[/]");

    public static void Info(string message) => 
        AnsiConsole.MarkupLine($"[{HelionPalette.Accent.ToMarkup()}]{HelionPalette.SymbolLightBlue} {Markup.Escape(message)}[/]");
}
