using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Uno.Extensions.Aspire.Hosting.Tests;

/// <summary>
/// A temporary AppHost folder holding an app project.
/// </summary>
internal sealed class TestSolution : IDisposable
{
	public static readonly string WebAssemblyTargetFramework = $"net{Environment.Version.Major}.0-browserwasm";

	public static readonly string AndroidTargetFramework = $"net{Environment.Version.Major}.0-android";

	// Builds instantly, and fails when a file named fail.txt sits next to it. Build comes first: it is the default target.
	public const string BuildTargets = """
		<Target Name="Build">
			<Error Condition="Exists('fail.txt')" Text="Requested failure." />
		</Target>
		<Target Name="Restore" />
		""";

	/// <param name="projectContent">What goes inside the project's root element, by default targets that build instantly.</param>
	public TestSolution(string projectContent = BuildTargets)
	{
		Folder = Path.Join(Path.GetTempPath(), "uno-aspire-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(AppFolder);
		File.WriteAllText(ProjectPath, $"<Project>{projectContent}</Project>");
	}

	public string Folder { get; }

	public string AppFolder => Path.Join(Folder, "MyWasmApp");

	public string ProjectPath => Path.Join(AppFolder, "MyWasmApp.csproj");

	public IDistributedApplicationBuilder CreateBuilder() =>
		DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [], ProjectDirectory = Folder });

	public IResourceBuilder<UnoWebAssemblyResource> AddWebAssembly(IDistributedApplicationBuilder builder) =>
		builder.AddUnoApp("app", ProjectPath)
			.AddWebAssembly()
			.WithEndpoint(UnoWebAssemblyResource.EndpointName, endpoint =>
			{
				endpoint.TargetPort = 5377;
				endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 5300);
			});

	public IResourceBuilder<UnoAndroidEmulatorResource> AddAndroidEmulator(IDistributedApplicationBuilder builder) =>
		builder.AddUnoApp("app", ProjectPath).AddAndroidEmulator();

	public void Dispose() => Directory.Delete(Folder, recursive: true);
}
