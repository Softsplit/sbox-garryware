using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private readonly Dictionary<string, int> spacing = new()
	{
		["0"] = 0,
		["px"] = 1,
		["0.5"] = 2,
		["1"] = 4,
		["2"] = 8,
		["4"] = 16,
		["8"] = 32
	};

	private void GenerateSpacingUtilities( StringBuilder sb )
	{
		foreach ( var (key, value) in spacing )
		{
			// Margin - no hover states
			GenerateUtility( sb, $"m-{key}", $"margin: {value}px" );
			GenerateUtility( sb, $"mx-{key}", $"margin-left: {value}px; margin-right: {value}px" );
			GenerateUtility( sb, $"my-{key}", $"margin-top: {value}px; margin-bottom: {value}px" );
			GenerateUtility( sb, $"mt-{key}", $"margin-top: {value}px" );
			GenerateUtility( sb, $"mr-{key}", $"margin-right: {value}px" );
			GenerateUtility( sb, $"mb-{key}", $"margin-bottom: {value}px" );
			GenerateUtility( sb, $"ml-{key}", $"margin-left: {value}px" );

			// Padding - with hover states
			GenerateUtility( sb, $"p-{key}", $"padding: {value}px", includeHover: true );
			GenerateUtility( sb, $"px-{key}", $"padding-left: {value}px; padding-right: {value}px", includeHover: true );
			GenerateUtility( sb, $"py-{key}", $"padding-top: {value}px; padding-bottom: {value}px", includeHover: true );
			GenerateUtility( sb, $"pt-{key}", $"padding-top: {value}px", includeHover: true );
			GenerateUtility( sb, $"pr-{key}", $"padding-right: {value}px", includeHover: true );
			GenerateUtility( sb, $"pb-{key}", $"padding-bottom: {value}px", includeHover: true );
			GenerateUtility( sb, $"pl-{key}", $"padding-left: {value}px", includeHover: true );

			// Gap - no hover states
			GenerateUtility( sb, $"gap-{key}", $"gap: {value}px" );
			GenerateUtility( sb, $"gap-x-{key}", $"column-gap: {value}px" );
			GenerateUtility( sb, $"gap-y-{key}", $"row-gap: {value}px" );
		}
	}
}
