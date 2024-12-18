using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private readonly Dictionary<string, string> sizes = new()
	{
		["0"] = "0",
		["px"] = "1px",
		["0.5"] = "2px",
		["1"] = "4px",
		["2"] = "8px",
		["3"] = "12px",
		["4"] = "16px",
		["5"] = "20px",
		["6"] = "24px",
		["8"] = "32px",
		["10"] = "40px",
		["12"] = "48px",
		["16"] = "64px",
		["20"] = "80px",
		["24"] = "96px",
		["32"] = "128px",
		["40"] = "160px",
		["48"] = "192px",
		["56"] = "224px",
		["64"] = "256px",
		["auto"] = "auto",
		["full"] = "100%",
		["screen"] = "100vh",
		// ["min"] = "min-content",
		// ["max"] = "max-content",
		// ["fit"] = "fit-content"
	};

	private void GenerateSizeUtilities( StringBuilder sb )
	{
		foreach ( var (key, value) in sizes )
		{
			GenerateUtility( sb, $"w-{key}", $"width: {value}" );
			GenerateUtility( sb, $"h-{key}", $"height: {value}" );
			GenerateUtility( sb, $"min-w-{key}", $"min-width: {value}" );
			GenerateUtility( sb, $"min-h-{key}", $"min-height: {value}" );
			GenerateUtility( sb, $"max-w-{key}", $"max-width: {value}" );
			GenerateUtility( sb, $"max-h-{key}", $"max-height: {value}" );
		}

		var percentages = new[] { "1/2", "1/3", "2/3", "1/4", "3/4", "1/5", "2/5", "3/5", "4/5" };
		var fractions = new[] { 0.5f, 0.333f, 0.667f, 0.25f, 0.75f, 0.2f, 0.4f, 0.6f, 0.8f };

		for ( var i = 0; i < percentages.Length; i++ )
		{
			GenerateUtility( sb, $"w-{percentages[i]}", $"width: {fractions[i] * 100}%" );
			GenerateUtility( sb, $"h-{percentages[i]}", $"height: {fractions[i] * 100}%" );
		}
	}
}
