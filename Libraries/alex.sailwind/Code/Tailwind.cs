using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;
using System.Text;

[Hide]
[Title( "Tailwind Panel" )]
public partial class TailwindPanelComponent : PanelComponent
{
	private void GenerateUtility( StringBuilder sb, string className, string cssProperties, bool includeHover = false )
	{
		sb.AppendLine( $".{className} {{ {cssProperties}; }}" );
		if ( includeHover )
		{
			sb.AppendLine( $".hover-{className}:hover {{ {cssProperties}; }}" );
		}
	}

	private List<string> FindCustomClasses( string prefix )
	{
		List<string> matches = new();

		void EnumerateChildren( Panel root )
		{
			foreach ( var panel in root.Children )
			{
				foreach ( var className in panel.Class )
				{
					if ( className.StartsWith( prefix ) && !matches.Contains( className ) )
					{
						matches.Add( className );
					}

					if ( className.StartsWith( "hover-" + prefix ) && !matches.Contains( className ) )
					{
						matches.Add( className );
					}
				}

				EnumerateChildren( panel );
			}
		}

		EnumerateChildren( Panel );

		return matches;
	}

	public string Generate()
	{
		var sb = new StringBuilder();

		GenerateSpacingUtilities( sb );
		GenerateColorUtilities( sb );
		GenerateFontUtilities( sb );
		GenerateFlexUtilities( sb );
		GenerateShadowUtilities( sb );
		GenerateRoundingUtilities( sb );
		GenerateTransitionUtilities( sb );
		GenerateSizeUtilities( sb );
		GenerateTransformUtilities( sb );

		return sb.ToString();
	}

	protected override void OnTreeFirstBuilt()
	{
		var styleCode = Generate();
		Panel.StyleSheet.Parse( styleCode );
	}
}
