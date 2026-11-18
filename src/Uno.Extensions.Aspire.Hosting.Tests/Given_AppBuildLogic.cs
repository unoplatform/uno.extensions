using System.Diagnostics;
using Aspire.Hosting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

/// <summary>
/// Runs the build logic of Uno.Extensions.Aspire with MSBuild against the file the AppHost writes.
/// </summary>
[TestClass]
public class Given_AppBuildLogic
{
	private const string WebAssemblyTarget = "GenerateUnoWasmAssets";
	private const string AndroidTarget = "Build";

	private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(2);

	private readonly TestSolution _solution = new();

	[TestCleanup]
	public void DeleteSolution() => _solution.Dispose();

	// What the app's build hands to the WebAssembly bootstrapper or the Android environment file, one NAME=value per line, and the build's output.
	private async Task<(string[] Environment, string Log)> BuildAppAsync(string? property = null, string target = WebAssemblyTarget)
	{
		var output = Path.Join(_solution.AppFolder, "environment.txt");
		File.Delete(output);

		var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
		{
			WorkingDirectory = _solution.AppFolder,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		foreach (var argument in new[] { "msbuild", _solution.ProjectPath, $"-t:{target}", "-nologo", "-nodeReuse:false" })
		{
			start.ArgumentList.Add(argument);
		}

		if (property is not null)
		{
			start.ArgumentList.Add($"-p:{property}");
		}

		using var process = Process.Start(start)!;
		var standardOutput = process.StandardOutput.ReadToEndAsync();
		var standardError = process.StandardError.ReadToEndAsync();
		using (var timeout = new CancellationTokenSource(BuildTimeout))
		{
			try
			{
				await process.WaitForExitAsync(timeout.Token);
			}
			catch (OperationCanceledException)
			{
				process.Kill(entireProcessTree: true);
				throw;
			}
		}

		var log = await standardOutput + await standardError;
		process.ExitCode.Should().Be(0, log);
		return (File.Exists(output) ? File.ReadAllLines(output) : [], log);
	}

	[TestMethod]
	public async Task When_AppIsBuiltWhileTheHeadRuns_Then_TheBuildCarriesTheValues()
	{
		const string value = "a;b$(c)%d'e&<\">@f*?\\g";
		var builder = _solution.CreateBuilder();
		var api = TestSolution.AddApi(builder);
		var wasm = _solution.AddWebAssembly(builder)
			.WithReference(api.GetEndpoint("http"))
			.WithEnvironment("Name", value)
			.WithClientEnvironment("Name");
		await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource);

		var (environment, log) = await BuildAppAsync();

		environment.Should().BeEquivalentTo([$"Name={value}", "services__api__http__0=http://localhost:5171"]);
		log.Should().Contain("Name, services__api__http__0").And.NotContain("a;b", "values may be secrets");
	}

	[TestMethod]
	public async Task When_AndroidAppIsBuiltWhileTheHeadRuns_Then_ItsEnvironmentFileCarriesTheValues()
	{
		var builder = _solution.CreateBuilder();
		var api = TestSolution.AddApi(builder);
		var android = _solution.AddAndroidEmulator(builder).WithReference(api.GetEndpoint("http"));
		await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource);

		var (environment, _) = await BuildAppAsync($"TargetFramework={TestSolution.AndroidTargetFramework}", AndroidTarget);

		environment.Should().Equal("SERVICES__API__HTTP__0=http://10.0.2.2:5171");
	}

	[TestMethod]
	public async Task When_AndroidSdkIgnoresTheValues_Then_TheBuildWarns()
	{
		var builder = _solution.CreateBuilder();
		var api = TestSolution.AddApi(builder);
		var android = _solution.AddAndroidEmulator(builder).WithReference(api.GetEndpoint("http"));
		await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource);

		var (_, log) = await BuildAppAsync($"TargetFramework={TestSolution.AndroidTargetFramework};SdkIgnoresRuntimeEnvironmentVariable=true", AndroidTarget);

		log.Should().Contain("warning").And.Contain("did not write the Aspire AppHost's environment into the app (SERVICES__API__HTTP__0)");
	}

	[TestMethod]
	public async Task When_AndroidAppIsBuiltOptimized_Then_ItsEnvironmentFileCarriesNothing()
	{
		var builder = _solution.CreateBuilder();
		var api = TestSolution.AddApi(builder);
		var android = _solution.AddAndroidEmulator(builder).WithReference(api.GetEndpoint("http"));
		await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource);

		var (environment, log) = await BuildAppAsync($"TargetFramework={TestSolution.AndroidTargetFramework};Optimize=true", AndroidTarget);

		environment.Should().BeEmpty();
		log.Should().NotContain("warning");
	}

	[TestMethod]
	[DataRow("Configuration=Staging", DisplayName = "Another configuration")]
	[DataRow("Optimize=true", DisplayName = "Optimized build")]
	[DataRow("_IsPublishing=true", DisplayName = "Publish")]
	[DataRow("TargetFramework=another-framework", DisplayName = "Another target framework")]
	public async Task When_BuildMustNotCarryTheValues_Then_ItDoesNot(string property)
	{
		var builder = _solution.CreateBuilder();
		var wasm = _solution.AddWebAssembly(builder).WithEnvironment("Name", "value").WithClientEnvironment("Name");
		await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource);

		var (environment, _) = await BuildAppAsync(property);

		environment.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_AppHostWroteNothing_Then_TheBuildCarriesNothing()
	{
		var (environment, log) = await BuildAppAsync();

		environment.Should().BeEmpty();
		log.Should().NotContain("Uno.Extensions.Aspire:");
	}

	[TestMethod]
	public async Task When_AppHostWroteAnotherFormat_Then_TheBuildWarnsAndCarriesNothing()
	{
		var otherFormat = _solution.ValuesPath.Replace(".v1.", ".v0.", StringComparison.Ordinal);
		Directory.CreateDirectory(Path.GetDirectoryName(otherFormat)!);
		File.WriteAllText(otherFormat, "<Project />");

		var (environment, log) = await BuildAppAsync();

		environment.Should().BeEmpty();
		log.Should().Contain("warning").And.Contain("same version of Uno.Extensions.Aspire and Uno.Extensions.Aspire.Hosting");
	}
}
