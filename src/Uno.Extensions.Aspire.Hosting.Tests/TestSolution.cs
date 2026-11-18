using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Extensions.Aspire.Hosting.Tests;

/// <summary>
/// A temporary AppHost folder holding an app project.
/// </summary>
internal sealed class TestSolution : IDisposable
{
	public static readonly string WebAssemblyTargetFramework = $"net{Environment.Version.Major}.0-browserwasm";

	public static readonly string AndroidTargetFramework = $"net{Environment.Version.Major}.0-android";

	public TestSolution()
	{
		Folder = Path.Join(Path.GetTempPath(), "uno-aspire-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(AppFolder);
		File.WriteAllText(ProjectPath, "<Project />");
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

	// Resolves the head's arguments and environment as a start does, without starting a process.
	public static async Task<IExecutionConfigurationResult> StartAsync(IDistributedApplicationBuilder builder, IResource resource) =>
		await ExecutionConfigurationBuilder.Create(resource)
			.WithArgumentsConfig()
			.WithEnvironmentVariablesConfig()
			.BuildAsync(builder.ExecutionContext, NullLogger.Instance, CancellationToken.None);

	public void Dispose() => Directory.Delete(Folder, recursive: true);
}
