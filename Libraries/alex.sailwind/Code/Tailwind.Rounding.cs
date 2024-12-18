using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private readonly Dictionary<string, int> rounding = new()
	{
		["none"] = 0,
		["sm"] = 2,
		["DEFAULT"] = 4,
		["md"] = 6,
		["lg"] = 8,
		["xl"] = 12,
		["2xl"] = 16,
		["3xl"] = 24,
		["full"] = 9999
	};
	private void GenerateRoundingUtilities( StringBuilder sb )
	{
		foreach ( var (key, value) in rounding )
		{
			var className = key == "DEFAULT" ? "rounded" : $"rounded-{key}";
			GenerateUtility( sb, className, $"border-radius: {value}px", includeHover: true );
		}
	}
}
