using System.Xml.Linq;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Aspire.Hosting.Tests;

/// <summary>
/// A temporary AppHost folder holding an app project, by default one that imports the build logic of Uno.Extensions.Aspire.
/// </summary>
internal sealed class TestSolution : IDisposable
{
	public static readonly string WebAssemblyTargetFramework = $"net{Environment.Version.Major}.0-browserwasm";

	public static readonly string AndroidTargetFramework = $"net{Environment.Version.Major}.0-android";

	// Builds instantly, and fails when a file named fail.txt sits next to it.
	public const string BuildTargets = """
		<Target Name="Build">
			<Error Condition="Exists('fail.txt')" Text="Requested failure." />
		</Target>
		<Target Name="Restore" />
		""";

	// Records what the WebAssembly bootstrapper or the Android environment file would get, instead of building the app.
	public static readonly string AppContent = $"""
		<PropertyGroup>
			<TargetFrameworks>{WebAssemblyTargetFramework};{AndroidTargetFramework}</TargetFrameworks>
			<TargetFramework Condition="'$(TargetFramework)' == ''">{WebAssemblyTargetFramework}</TargetFramework>
			<Configuration Condition="'$(Configuration)' == ''">Debug</Configuration>
			<TargetPlatformIdentifier Condition="$(TargetFramework.EndsWith('-android'))">android</TargetPlatformIdentifier>
			<IntermediateOutputPath>obj\$(Configuration)\$(TargetFramework)\</IntermediateOutputPath>
			<_PlatformBuild Condition="'$(TargetPlatformIdentifier)' == 'android'">_GenerateEnvironmentFiles</_PlatformBuild>
			<_PlatformBuild Condition="'$(TargetPlatformIdentifier)' != 'android'">GenerateUnoWasmAssets</_PlatformBuild>
		</PropertyGroup>
		<Import Project="{Path.Join(AppContext.BaseDirectory, "Uno.Extensions.Aspire.targets")}" />
		<Target Name="GenerateUnoWasmAssets">
			<WriteLinesToFile File="environment.txt" Lines="@(WasmShellMonoEnvironment->'%(Identity)=%(Value)')" Overwrite="true" />
		</Target>
		<!-- As .NET for Android does, unless the property stands in for an SDK that ignores the items. -->
		<Target Name="_GenerateEnvironmentFiles">
			<ItemGroup Condition="'$(SdkIgnoresRuntimeEnvironmentVariable)' != 'true'">
				<_Line Include="@(RuntimeEnvironmentVariable->'%(Identity)=%(Value)')" />
			</ItemGroup>
			<WriteLinesToFile File="$(IntermediateOutputPath)__environment__.txt" Lines="mono.enable_assembly_preload=0;@(_Line)" Overwrite="true" />
			<WriteLinesToFile File="environment.txt" Lines="@(_Line)" Overwrite="true" />
		</Target>
		<Target Name="Build" DependsOnTargets="$(_PlatformBuild)">
			<Error Condition="Exists('fail.txt')" Text="Requested failure." />
		</Target>
		<Target Name="Restore" />
		""";

	/// <param name="projectContent">What goes inside the project's root element.</param>
	public TestSolution(string? projectContent = null)
	{
		Folder = Path.Join(Path.GetTempPath(), "uno-aspire-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(AppFolder);
		// Build is named: an imported file's first target would otherwise be the default one.
		File.WriteAllText(ProjectPath, $"""<Project DefaultTargets="Build">{projectContent ?? AppContent}</Project>""");
	}

	public string Folder { get; }

	public string AppFolder => Path.Join(Folder, "MyWasmApp");

	public string ProjectPath => Path.Join(AppFolder, "MyWasmApp.csproj");

	public string ValuesPath => ValuesPathFor(WebAssemblyTargetFramework);

	public string ValuesPathFor(string targetFramework) => Path.Join(AppFolder, "obj", "uno-aspire", $"{targetFramework}.v1.values.proj");

	public IDistributedApplicationBuilder CreateBuilder()
	{
		var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [], ProjectDirectory = Folder });
		// The dev server runs the uno.devserver CLI: Given_UnoDevServer covers it without starting one.
		builder.Configuration[UnoDevServer.EnabledSetting] = "false";
		return builder;
	}

	public IResourceBuilder<UnoWebAssemblyResource> AddWebAssembly(IDistributedApplicationBuilder builder) =>
		builder.AddUnoProject("app", ProjectPath)
			.AddWebAssembly()
			.WithEndpoint(UnoWebAssemblyResource.EndpointName, endpoint =>
			{
				endpoint.TargetPort = 5377;
				endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 5300);
			});

	public IResourceBuilder<UnoAndroidEmulatorResource> AddAndroidEmulator(IDistributedApplicationBuilder builder) =>
		builder.AddUnoProject("app", ProjectPath).AddAndroidEmulator();

	public static IResourceBuilder<ExecutableResource> AddApi(IDistributedApplicationBuilder builder) =>
		builder.AddExecutable("api", "api", ".")
			.WithHttpEndpoint(name: "http")
			.WithEndpoint("http", endpoint => endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 5171));

	/// <summary>
	/// Writes the head's client environment as its start does, without building or starting it.
	/// </summary>
	public static Task WriteClientEnvironmentAsync(IDistributedApplicationBuilder builder, UnoHeadResource head, ILogger? logger = null) =>
		ClientEnvironment.WriteAsync(
			head,
			head is UnoAndroidEmulatorResource ? BakedEnvironment.ForAndroidEmulator : BakedEnvironment.ForWebAssembly,
			builder.ExecutionContext,
			logger ?? new ListLogger(),
			CancellationToken.None);

	public Dictionary<string, string> BakedVariables(string? targetFramework = null) =>
		XElement.Load(ValuesPathFor(targetFramework ?? WebAssemblyTargetFramework))
			.Descendants("_UnoAspireEnvironment")
			.ToDictionary(item => item.Attribute("Include")!.Value, item => Uri.UnescapeDataString(item.Attribute("Value")!.Value));

	public void Dispose() => Directory.Delete(Folder, recursive: true);
}
