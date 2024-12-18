using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private readonly Dictionary<string, string> shadows = new()
	{
		["sm"] = "0 1px 2px 0 rgb(0 0 0 / 0.05)",
		["DEFAULT"] = "0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)",
		["md"] = "0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)",
		["lg"] = "0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)",
		["xl"] = "0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)",
		["2xl"] = "0 25px 50px -12px rgb(0 0 0 / 0.25)",
	};

	private void GenerateShadowUtilities( StringBuilder sb )
	{
		foreach ( var (key, value) in shadows )
		{
			var className = key == "DEFAULT" ? "shadow" : $"shadow-{key}";
			GenerateUtility( sb, className, $"box-shadow: {value}", includeHover: true );
		}
	}
}
