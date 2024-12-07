using System.Collections.Generic;
using System.Linq;
using Editor;
using Microsoft.CodeAnalysis;
using Sandbox;
using Application = Editor.Application;

[Dock( "Editor", "Error List", "report" )]
public class ErrorList : Widget
{
	// Static because the widget can be deleted on hide/show & hotload
	public static readonly List<Diagnostic> Diagnostics = new();
	public ErrorListView ErrorListView;
	public Button ErrorsButton;
	public Button InfoButton;

	private bool ShowErrors = true;
	private bool ShowInfo = true;
	private bool ShowWarnings = true;

	public Button WarningsButton;

	public ErrorList( Widget parent ) : base( parent )
	{
		Name = "ErrorList";

		Layout = Layout.Column();

		var layout = Layout.Add( Layout.Row() );
		layout.Spacing = 8;
		layout.Margin = 5;

		ErrorsButton = new Button( "0 Errors", "error", this )
		{
			Clicked = () =>
			{
				ShowErrors = !ShowErrors;
				UpdateErrors();
				ErrorsButton.Update();
			},
			OnPaintOverride = () => PaintShittyButton( ErrorsButton, "error", ErrorColor, ShowErrors ),
			StatusTip = "Toggle display of errors"
		};

		WarningsButton = new Button( "0 Warnings", "warning", this )
		{
			Clicked = () =>
			{
				ShowWarnings = !ShowWarnings;
				UpdateErrors();
				WarningsButton.Update();
			},
			OnPaintOverride = () => PaintShittyButton( WarningsButton, "warning", WarningColor, ShowWarnings ),
			StatusTip = "Toggle display of warnings"
		};


		InfoButton = new Button( "0 Messages", "info", this )
		{
			Clicked = () =>
			{
				ShowInfo = !ShowInfo;
				UpdateErrors();
				InfoButton.Update();
			},
			OnPaintOverride = () => PaintShittyButton( InfoButton, "info", InfoColor, ShowInfo ),
			StatusTip = "Toggle display of information"
		};

		layout.Add( ErrorsButton );
		layout.Add( WarningsButton );
		layout.Add( InfoButton );

		layout.AddStretchCell();

		var clearButton = new Button( "", "delete", this )
		{
			Tint = Color.Gray,
			Clicked = () =>
			{
				Diagnostics.Clear();
				UpdateErrors();
			},
			StatusTip = "Clear error list"
		};
		clearButton.SetProperty( "cssClass", "clear" );
		layout.Add( clearButton );

		ErrorListView = new ErrorListView( this );
		Layout.Add( ErrorListView, 1 );

		UpdateErrors();
	}

	internal static Color WarningColor => Theme.Yellow;
	internal static Color ErrorColor => Theme.Red;
	internal static Color InfoColor => Theme.Blue;

	[Event( "compile.complete" )]
	public static void CaptureDiagnostics( CompileGroup compileGroup )
	{
		Diagnostics.Clear();
		Diagnostics.AddRange( compileGroup.Compilers.Where( x => x.Diagnostics != null )
			.SelectMany( x => x.Diagnostics ) );

		// Grab a total count of all errors, update status bar and pop up errors list if they have some
		var errors = Diagnostics.Where( a => a.Severity == DiagnosticSeverity.Error ).ToArray();
		if ( errors.Any() )
		{
			EditorWindow?.StatusBar.ShowMessage( $"Build failed - you have {errors.Count()} errors", 10 );

			// Pop-up the error list if we have any errors
			// EditorWindow.ErrorListDock?.Show(); // Opens it if it's not already open
			// EditorWindow.ErrorListDock?.Raise(); // Switches any tab to it
		}
	}

	[Event( "compile.complete", Priority = 10 )]
	public void OnCompileComplete( CompileGroup _ )
	{
		// CaptureDiagnostics fills in the static Diagnostics list
		// which is the diagostics from the most recent compile.
		// which is all we care about, really
		UpdateErrors();
	}

	public void UpdateErrors()
	{
		// Convert Diagnostics to ProjectDiagnostic objects
		var q = Diagnostics
			.AsEnumerable()
			.Where( x => x.Severity != DiagnosticSeverity.Hidden )
			.Select( x => new ProjectDiagnostic( x ) ); // Map to ProjectDiagnostic

		WarningsButton.Text = $"{q.Count( x => x.OriginalDiagnostic.Severity == DiagnosticSeverity.Warning )} Warnings";
		ErrorsButton.Text = $"{q.Count( x => x.OriginalDiagnostic.Severity == DiagnosticSeverity.Error )} Errors";
		InfoButton.Text = $"{q.Count( x => x.OriginalDiagnostic.Severity == DiagnosticSeverity.Info )} Messages";

		if ( !ShowErrors )
		{
			q = q.Where( x => x.OriginalDiagnostic.Severity != DiagnosticSeverity.Error );
		}

		if ( !ShowWarnings )
		{
			q = q.Where( x => x.OriginalDiagnostic.Severity != DiagnosticSeverity.Warning );
		}

		if ( !ShowInfo )
		{
			q = q.Where( x => x.OriginalDiagnostic.Severity != DiagnosticSeverity.Info );
		}

		q = q.OrderByDescending( x => x.OriginalDiagnostic.Severity == DiagnosticSeverity.Error );

		ErrorListView.SetItems( q );
	}

	private bool PaintShittyButton( Button btn, string icon, Color color, bool active )
	{
		var rect = btn.LocalRect;

		Paint.SetBrush( Theme.Primary.WithAlpha( Paint.HasMouseOver ? 0.2f : 0.1f ) );
		Paint.ClearPen();

		if ( active )
		{
			Paint.SetPen( Theme.Primary.WithAlpha( 0.4f ), 2.0f );
			Paint.DrawRect( rect, 2 );
		}

		rect = rect.Shrink( 8, 3 );

		Paint.Antialiasing = true;
		Paint.SetPen( color.WithAlpha( Paint.HasMouseOver ? 1 : 0.7f ), 3.0f );
		Paint.ClearBrush();

		// Severity Icon
		var iconRect = rect;
		iconRect.Left += 0;
		iconRect.Width = 16;
		Paint.DrawIcon( iconRect, icon, 16 );

		rect.Left = iconRect.Right + 2;
		Paint.SetDefaultFont();
		Paint.SetPen( Theme.White.WithAlpha( active ? 1 : 0.4f ), 3.0f );
		Paint.DrawText( rect, btn.Text );

		return true;
	}
}

public class ErrorListView : ListView
{
	public ErrorListView( Widget parent ) : base( parent )
	{
		Name = "Output";

		ItemActivated = a =>
		{
			if ( a is ProjectDiagnostic diagnostic )
			{
				CodeEditor.OpenFile( diagnostic.FilePath, diagnostic.LineNumber, diagnostic.CharNumber );
			}
		};

		ItemContextMenu = OpenItemContextMenu;
		ItemSize = new Vector2( 0, 48 );
		ItemSpacing = 0;
		Margin = 0;
	}

	private void OpenItemContextMenu( object item )
	{
		if ( item is not ProjectDiagnostic diagnostic )
		{
			return;
		}

		var m = new Menu();

		m.AddOption( "Open in Code Editor", "file_open",
			() => CodeEditor.OpenFile( diagnostic.FilePath, diagnostic.LineNumber, diagnostic.CharNumber ) );
		m.AddOption( "Copy Error", "content_copy", () => EditorUtility.Clipboard.Copy( diagnostic.Message ) );

		m.OpenAt( Application.CursorPosition );
	}

	protected override void PaintItem( VirtualWidget item )
	{
		if ( item.Object is not ProjectDiagnostic diagnostic )
		{
			return;
		}

		var color = diagnostic.OriginalDiagnostic.Severity switch
		{
			DiagnosticSeverity.Error => ErrorList.ErrorColor,
			DiagnosticSeverity.Warning => ErrorList.WarningColor,
			_ => ErrorList.InfoColor
		};
		var icon = diagnostic.OriginalDiagnostic.Severity switch
		{
			DiagnosticSeverity.Error => "error",
			DiagnosticSeverity.Warning => "warning",
			_ => "info"
		};

		Paint.SetBrush( color.WithAlpha( Paint.HasMouseOver ? 0.3f : 0.2f ).Darken( 0.4f ) );
		Paint.ClearPen();
		Paint.DrawRect( item.Rect.Shrink( 0, 1 ) );

		Paint.Antialiasing = true;
		Paint.SetPen( color.WithAlpha( Paint.HasMouseOver ? 1 : 0.7f ), 3.0f );
		Paint.ClearBrush();

		// Severity Icon
		var iconRect = item.Rect.Shrink( 12, 0 );
		iconRect.Width = 24;
		Paint.DrawIcon( iconRect, icon, 24 );

		var rect = item.Rect.Shrink( 48, 8, 0, 8 );

		Paint.SetPen( Theme.White.WithAlpha( Paint.HasMouseOver ? 1 : 0.8f ), 3.0f );
		Paint.DrawText( rect, diagnostic.Message, TextFlag.LeftTop | TextFlag.SingleLine );

		Paint.SetPen( Theme.White.WithAlpha( Paint.HasMouseOver ? 0.5f : 0.4f ), 3.0f );
		Paint.DrawText( rect,
			$"{Project.Current.Config.Title} - {diagnostic.FilePath}({diagnostic.LineNumber},{diagnostic.CharNumber})",
			TextFlag.LeftBottom | TextFlag.SingleLine );
	}
}
