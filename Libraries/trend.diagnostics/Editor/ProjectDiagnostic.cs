using Microsoft.CodeAnalysis;

public class ProjectDiagnostic
{
	public readonly int CharNumber;
	public readonly string FilePath;
	public readonly int LineNumber;
	public readonly string Message;

	public ProjectDiagnostic( Diagnostic diag )
	{
		var span = diag.Location.GetLineSpan();
		var mappedSpan = diag.Location.GetMappedLineSpan();

		Message = diag.GetMessage();
		FilePath = mappedSpan.HasMappedPath ? mappedSpan.Path : span.Path;
		LineNumber = mappedSpan.Span.Start.Line + 1;
		CharNumber = mappedSpan.Span.Start.Character + 1;

		OriginalDiagnostic = diag;
	}

	public Diagnostic OriginalDiagnostic { get; }
}
