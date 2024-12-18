using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private readonly Dictionary<string, string> transitions = new()
	{
		["none"] = "none",
		["DEFAULT"] = "all", // "color, background-color, border-color, text-decoration-color, fill, stroke, opacity, box-shadow, transform, filter, backdrop-filter",
		["all"] = "all",
		["colors"] = "color, background-color, border-color, text-decoration-color, fill, stroke", // THis fucks up
		["opacity"] = "opacity",
		["shadow"] = "box-shadow",
		["transform"] = "transform"
	};

	private void GenerateTransitionUtilities( StringBuilder sb )
	{
		foreach ( var (key, value) in transitions )
		{
			var className = key == "DEFAULT" ? "transition" : $"transition-{key}";
			GenerateUtility( sb, className, $"transition: {value} 150ms ease;", includeHover: true );
		}
	}
}
