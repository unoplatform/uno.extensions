using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The dev server the heads of one solution connect to for Hot Reload: the one already running for the solution, or one
/// the AppHost starts.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
internal sealed class UnoDevServerResource(string name, UnoProjectResource project, string? solutionDirectory) : Resource(name)
{
	private readonly TaskCompletionSource<int?> _port = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private volatile bool _isStarting;

	/// <summary>
	/// The first app of the solution: its restore gives the version of the uno.devserver CLI.
	/// </summary>
	internal UnoProjectResource Project { get; } = project;

	/// <summary>
	/// The folder of the solution, where the uno.devserver CLI runs; <c>null</c> when the app is in no solution.
	/// </summary>
	internal string? SolutionDirectory { get; } = solutionDirectory;

	/// <summary>
	/// The folder that identifies this dev server: the solution's, or the app's when it is in no solution.
	/// </summary>
	internal string Folder => SolutionDirectory ?? Project.ProjectDirectory;

	/// <summary>
	/// The port the heads connect to, null when no dev server is available.
	/// </summary>
	internal Task<int?> Port => _port.Task;

	/// <summary>
	/// Whether the AppHost run started looking for the dev server. A head built outside a run does not wait for a port.
	/// </summary>
	internal bool IsStarting => _isStarting;

	internal void MarkStarting() => _isStarting = true;

	internal void SetPort(int? port) => _port.TrySetResult(port);
}
