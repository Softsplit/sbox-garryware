using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private readonly Dictionary<string, string> transforms = new()
	{
		// Scale
		["scale-0"] = "scale(0)",
		["scale-50"] = "scale(0.5)",
		["scale-75"] = "scale(0.75)",
		["scale-90"] = "scale(0.9)",
		["scale-95"] = "scale(0.95)",
		["scale-100"] = "scale(1)",
		["scale-105"] = "scale(1.05)",
		["scale-110"] = "scale(1.1)",
		["scale-125"] = "scale(1.25)",
		["scale-150"] = "scale(1.5)",
		["-scale-50"] = "scale(-0.5)",
		["-scale-75"] = "scale(-0.75)",
		["-scale-90"] = "scale(-0.9)",
		["-scale-95"] = "scale(-0.95)",
		["-scale-100"] = "scale(-1)",
		["-scale-105"] = "scale(-1.05)",
		["-scale-110"] = "scale(-1.1)",
		["-scale-125"] = "scale(-1.25)",
		["-scale-150"] = "scale(-1.5)",

		// Rotate
		["rotate-0"] = "rotate(0deg)",
		["rotate-45"] = "rotate(45deg)",
		["rotate-90"] = "rotate(90deg)",
		["rotate-180"] = "rotate(180deg)",
		["-rotate-45"] = "rotate(-45deg)",
		["-rotate-90"] = "rotate(-90deg)",
		["-rotate-180"] = "rotate(-180deg)",

		// Translate
		["translate-0"] = "translate(0)",
		["translate-x-0"] = "translateX(0)",
		["translate-y-0"] = "translateY(0)",
		["translate-x-1"] = "translateX(4px)",
		["translate-x-2"] = "translateX(8px)",
		["translate-x-4"] = "translateX(16px)",
		["translate-x-8"] = "translateX(32px)",
		["translate-y-1"] = "translateY(4px)",
		["translate-y-2"] = "translateY(8px)",
		["translate-y-4"] = "translateY(16px)",
		["translate-y-8"] = "translateY(32px)",
		["-translate-x-1"] = "translateX(-4px)",
		["-translate-x-2"] = "translateX(-8px)",
		["-translate-x-4"] = "translateX(-16px)",
		["-translate-x-8"] = "translateX(-32px)",
		["-translate-y-1"] = "translateY(-4px)",
		["-translate-y-2"] = "translateY(-8px)",
		["-translate-y-4"] = "translateY(-16px)",
		["-translate-y-8"] = "translateY(-32px)"
	};

	private void GenerateTransformUtilities( StringBuilder sb )
	{
		foreach ( var (key, value) in transforms )
		{
			var className = key.StartsWith( "-" ) ? $"transform-neg{key.Substring( 1 )}" : $"transform-{key}";
			GenerateUtility( sb, className, $"transform: {value}", includeHover: true );
		}

		var origins = new[] { "center", "top", "right", "bottom", "left", "top-right", "bottom-right", "bottom-left", "top-left" };
		foreach ( var origin in origins )
		{
			GenerateUtility( sb, $"origin-{origin}", $"transform-origin: {origin.Replace( "-", " " )}", includeHover: true );
		}
	}
}
