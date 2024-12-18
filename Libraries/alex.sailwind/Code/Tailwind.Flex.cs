using System.Collections.Generic;
using System.Text;

partial class TailwindPanelComponent
{
	private void GenerateFlexUtilities( StringBuilder sb )
	{
		// Core flex utilities - no hover states needed
		GenerateUtility( sb, "flex", "display: flex" );
		GenerateUtility( sb, "flex-row", "flex-direction: row" );
		GenerateUtility( sb, "flex-col", "flex-direction: column" );

		// Alignment utilities - might want hover states for dynamic layouts
		GenerateUtility( sb, "items-start", "align-items: flex-start", includeHover: true );
		GenerateUtility( sb, "items-center", "align-items: center", includeHover: true );
		GenerateUtility( sb, "items-end", "align-items: flex-end", includeHover: true );

		GenerateUtility( sb, "justify-start", "justify-content: flex-start", includeHover: true );
		GenerateUtility( sb, "justify-center", "justify-content: center", includeHover: true );
		GenerateUtility( sb, "justify-end", "justify-content: flex-end", includeHover: true );
		GenerateUtility( sb, "justify-between", "justify-content: space-between", includeHover: true );
	}
}
