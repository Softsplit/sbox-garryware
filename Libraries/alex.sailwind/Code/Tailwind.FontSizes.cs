using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private readonly Dictionary<string, int> fontSizes = new()
	{
		["xs"] = 12,
		["sm"] = 14,
		["base"] = 16,
		["lg"] = 18,
		["xl"] = 20,
		["2xl"] = 24
	};

	private void GenerateFontUtilities( StringBuilder sb )
	{
		foreach ( var (name, size) in fontSizes )
		{
			GenerateUtility( sb, $"text-{name}", $"font-size: {size}px", includeHover: true );
		}

		// Font weights with hover
		GenerateUtility( sb, "font-normal", "font-weight: 400", includeHover: true );
		GenerateUtility( sb, "font-medium", "font-weight: 500", includeHover: true );
		GenerateUtility( sb, "font-bold", "font-weight: 700", includeHover: true );
	}
}
