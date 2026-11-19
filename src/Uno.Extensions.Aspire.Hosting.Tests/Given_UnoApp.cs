using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

[TestClass]
public class Given_UnoApp
{
	private static readonly string ProjectPath = Path.Join(AppContext.BaseDirectory, "MyApp", "MyApp.csproj");

	[ClassInitialize]
	public static void CreateProjectFile(TestContext _)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(ProjectPath)!);
		File.WriteAllText(ProjectPath, "<Project />");
	}

	[TestMethod]
	public void When_AppAdded_Then_OnlyItsHeadsAreInTheModel()
	{
		var builder = DistributedApplication.CreateBuilder();

		var app = builder.AddUnoApp("app", ProjectPath);
		app.AddDesktop();

		builder.Resources.Select(resource => resource.Name).Should().Equal("app-desktop");
	}

	[TestMethod]
	public void When_ProjectNamesItsFrameworks_Then_EachHeadUsesItsOwn()
	{
		using var solution = new TestSolution("<PropertyGroup><TargetFrameworks>net9.0-android;net9.0-browserwasm;net9.0-desktop</TargetFrameworks></PropertyGroup>");
		var builder = solution.CreateBuilder();

		var app = builder.AddUnoApp("app", solution.ProjectPath);

		app.AddDesktop().Resource.TargetFramework.Should().Be("net9.0-desktop");
		app.AddWebAssembly().Resource.TargetFramework.Should().Be("net9.0-browserwasm");
		app.AddAndroidEmulator().Resource.TargetFramework.Should().Be("net9.0-android");
	}

	[TestMethod]
	public void When_TheFrameworkCarriesAPlatformVersion_Then_TheHeadStillUsesIt()
	{
		using var solution = new TestSolution("<PropertyGroup><TargetFrameworks>net10.0-android36.0;net10.0-desktop</TargetFrameworks></PropertyGroup>");
		var builder = solution.CreateBuilder();

		var android = builder.AddUnoApp("app", solution.ProjectPath).AddAndroidEmulator();

		android.Resource.TargetFramework.Should().Be("net10.0-android36.0");
	}

	[TestMethod]
	public void When_AGroupAppendsToTheList_Then_ItsFrameworksAreKept()
	{
		using var solution = new TestSolution("""
			<PropertyGroup><TargetFrameworks>net9.0-desktop</TargetFrameworks></PropertyGroup>
			<PropertyGroup Condition="'$(OS)' == 'Windows_NT'"><TargetFrameworks>$(TargetFrameworks);net9.0-android</TargetFrameworks></PropertyGroup>
			""");
		var builder = solution.CreateBuilder();

		var app = builder.AddUnoApp("app", solution.ProjectPath);

		app.Resource.TargetFrameworks.Should().Equal("net9.0-desktop", "net9.0-android");
	}

	[TestMethod]
	public void When_ProjectComputesItsFrameworks_Then_HeadsUseTheAppHostVersion()
	{
		using var solution = new TestSolution("<PropertyGroup><TargetFrameworks>$(DotNetVersion)-desktop</TargetFrameworks></PropertyGroup>");
		var builder = solution.CreateBuilder();

		var desktop = builder.AddUnoApp("app", solution.ProjectPath).AddDesktop();

		desktop.Resource.TargetFramework.Should().Be($"net{Environment.Version.Major}.0-desktop");
	}

	[TestMethod]
	public void When_ProjectFileIsMissing_Then_AddUnoAppThrows()
	{
		var builder = DistributedApplication.CreateBuilder();
		var missingPath = Path.Join(AppContext.BaseDirectory, "missing", "Missing.csproj");

		var add = () => builder.AddUnoApp("app", missingPath);

		add.Should().Throw<DistributedApplicationException>().WithMessage($"*{missingPath}*");
	}

	[TestMethod]
	public void When_ProjectFileIsMissingInPublishMode_Then_AddUnoAppDoesNotThrow()
	{
		var builder = DistributedApplication.CreateBuilder(["--publisher", "manifest", "--output-path", Path.Join(AppContext.BaseDirectory, "manifest")]);

		var add = () => builder.AddUnoApp("app", Path.Join(AppContext.BaseDirectory, "missing", "Missing.csproj"));

		add.Should().NotThrow();
	}

	[TestMethod]
	public void When_ProjectPathIsRelative_Then_ItResolvesFromTheAppHostFolder()
	{
		var builder = DistributedApplication.CreateBuilder();
		var relativePath = Path.GetRelativePath(builder.AppHostDirectory, ProjectPath);
		Path.IsPathRooted(relativePath).Should().BeFalse();

		var app = builder.AddUnoApp("app", relativePath);

		app.Resource.ProjectPath.Should().Be(ProjectPath);
	}

	[TestMethod]
	public async Task When_DesktopAdded_Then_ItRunsTheDesktopFrameworkWithoutALaunchProfile()
	{
		var builder = DistributedApplication.CreateBuilder();

		var desktop = builder.AddUnoApp("app", ProjectPath).AddDesktop();

		desktop.Resource.Command.Should().Be("dotnet");
		desktop.Resource.WorkingDirectory.Should().Be(Path.GetDirectoryName(ProjectPath));
		var args = (await ExecutionConfiguration.ResolveAsync(builder, desktop.Resource)).Arguments.Select(argument => argument.Value);
		args.Should().Equal("run", "--project", ProjectPath, "-f", $"net{Environment.Version.Major}.0-desktop", "--no-build", "--no-launch-profile");
	}

	[TestMethod]
	public async Task When_FrameworkAndProfileSet_Then_TheRunArgumentsUseThem()
	{
		var builder = DistributedApplication.CreateBuilder();

		var desktop = builder.AddUnoApp("app", ProjectPath)
			.AddDesktop("ui")
			.WithTargetFramework("net9.0-desktop")
			.WithLaunchProfile("MyApp (Desktop)");

		desktop.Resource.Name.Should().Be("ui");
		var args = (await ExecutionConfiguration.ResolveAsync(builder, desktop.Resource)).Arguments.Select(argument => argument.Value);
		args.Should().Equal("run", "--project", ProjectPath, "-f", "net9.0-desktop", "--no-build", "--launch-profile", "MyApp (Desktop)");
	}

	[TestMethod]
	public async Task When_DesktopAdded_Then_ItsEnvironmentAllowsHotReloadAndExportsTelemetry()
	{
		var builder = DistributedApplication.CreateBuilder();

		var desktop = builder.AddUnoApp("app", ProjectPath).AddDesktop();

		var environment = (await ExecutionConfiguration.ResolveAsync(builder, desktop.Resource)).EnvironmentVariables;
		environment.Should().Contain("DOTNET_MODIFIABLE_ASSEMBLIES", "debug");
		environment.Should().Contain("DOTNET_ENVIRONMENT", "Development");
		environment.Should().ContainKey("OTEL_EXPORTER_OTLP_ENDPOINT");
		environment.Should().ContainKey("OTEL_SERVICE_NAME");
	}

	[TestMethod]
	public async Task When_DesktopReferencesAnEndpoint_Then_ItsEnvironmentCarriesTheAddress()
	{
		var builder = DistributedApplication.CreateBuilder();
		var api = builder.AddExecutable("api", "api", ".")
			.WithHttpEndpoint(name: "http")
			.WithEndpoint("http", endpoint => endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 5171));

		var desktop = builder.AddUnoApp("app", ProjectPath).AddDesktop().WithReference(api.GetEndpoint("http"));

		var environment = (await ExecutionConfiguration.ResolveAsync(builder, desktop.Resource)).EnvironmentVariables;
		environment.Should().Contain("services__api__http__0", "http://localhost:5171");
	}

	[TestMethod]
	public void When_DesktopAdded_Then_ItStartsWithTheAppHostAndStaysOutOfTheManifest()
	{
		var builder = DistributedApplication.CreateBuilder();

		var desktop = builder.AddUnoApp("app", ProjectPath).AddDesktop();

		desktop.Resource.Annotations.OfType<ExplicitStartupAnnotation>().Should().BeEmpty();
		desktop.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().Should().Equal(ManifestPublishingCallbackAnnotation.Ignore);
	}
}
