using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Extensions that add an Uno Platform app and its heads to an Aspire application model.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public static class UnoAppBuilderExtensions
{
	private const string ModifiableAssembliesVariable = "DOTNET_MODIFIABLE_ASSEMBLIES";
	private const string ServerUrlsVariable = "ASPNETCORE_URLS";

	// Deploys to the running emulator, not to a connected device.
	private const string EmulatorTargetArgument = "-p:AdbTarget=-e";

	/// <summary>
	/// Adds an Uno Platform app. It starts nothing until a head is added, for example with <see cref="AddDesktop"/>.
	/// </summary>
	/// <remarks>
	/// The app is not a resource of the model: references, waits and environment are set on its heads.
	/// </remarks>
	/// <param name="builder">The application builder.</param>
	/// <param name="name">The name of the resource.</param>
	/// <param name="projectPath">The path of the app's project file, relative to the AppHost folder or absolute.</param>
	/// <returns>A builder for the app.</returns>
	public static IResourceBuilder<UnoAppResource> AddUnoApp(
		this IDistributedApplicationBuilder builder,
		[ResourceName] string name,
		string projectPath)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(projectPath);

		var fullPath = Path.GetFullPath(projectPath, builder.AppHostDirectory);
		IReadOnlyList<string> targetFrameworks = [];
		// Heads are left out of a publish, which may run where the app is not checked out.
		if (builder.ExecutionContext.IsRunMode)
		{
			if (!File.Exists(fullPath))
			{
				throw new DistributedApplicationException($"The project file of the Uno app '{name}' was not found at '{fullPath}'.");
			}

			targetFrameworks = ProjectTargetFrameworks.Read(fullPath);
		}

		// Not added to the model: the app runs nothing, only its heads appear in the dashboard.
		return builder.CreateResourceBuilder(new UnoAppResource(name, fullPath, targetFrameworks));
	}

	/// <summary>
	/// Adds the desktop head of the app. It starts with the AppHost.
	/// </summary>
	/// <remarks>
	/// The target framework is the project's <c>-desktop</c> one, or the AppHost's .NET version when the project computes
	/// its target frameworks from properties. Use <see cref="WithTargetFramework"/> to choose another one.
	/// </remarks>
	/// <param name="builder">The app builder.</param>
	/// <param name="name">The name of the head, <c>&lt;app&gt;-desktop</c> when omitted.</param>
	/// <returns>A builder for the head.</returns>
	public static IResourceBuilder<UnoDesktopResource> AddDesktop(
		this IResourceBuilder<UnoAppResource> builder,
		[ResourceName] string? name = null)
	{
		ArgumentNullException.ThrowIfNull(builder);

		var head = new UnoDesktopResource(name ?? $"{builder.Resource.Name}-desktop", builder.Resource);

		return builder.ApplicationBuilder.AddHead(head)
			.WithArgs(context => AddRunArguments(head, context.Args))
			.WithOtlpExporter();
	}

	/// <summary>
	/// Adds the WebAssembly head of the app. It is started from the dashboard, not with the AppHost.
	/// </summary>
	/// <remarks>
	/// The app is served on the endpoint <see cref="UnoWebAssemblyResource.EndpointName"/>, and the API has to allow
	/// that origin in its CORS policy.
	/// </remarks>
	/// <param name="builder">The app builder.</param>
	/// <param name="name">The name of the head, <c>&lt;app&gt;-wasm</c> when omitted.</param>
	/// <returns>A builder for the head.</returns>
	public static IResourceBuilder<UnoWebAssemblyResource> AddWebAssembly(
		this IResourceBuilder<UnoAppResource> builder,
		[ResourceName] string? name = null)
	{
		ArgumentNullException.ThrowIfNull(builder);

		var head = new UnoWebAssemblyResource(name ?? $"{builder.Resource.Name}-wasm", builder.Resource);

		return builder.ApplicationBuilder.AddHead(head)
			.WithArgs(context => AddRunArguments(head, context.Args))
			.WithHttpEndpoint(name: UnoWebAssemblyResource.EndpointName)
			.WithEnvironment(context => context.EnvironmentVariables[ServerUrlsVariable] =
				ReferenceExpression.Create($"http://localhost:{head.GetEndpoint(UnoWebAssemblyResource.EndpointName).Property(EndpointProperty.TargetPort)}"))
			// Healthy once the app is served.
			.WithHttpHealthCheck("/", endpointName: UnoWebAssemblyResource.EndpointName)
			// Started by hand, so a run does not build every head.
			.WithExplicitStart();
	}

	/// <summary>
	/// Adds the Android head of the app, deployed to the running emulator. It is started from the dashboard, not with the AppHost.
	/// </summary>
	/// <remarks>
	/// Exactly one emulator has to be running; booting it is not part of the head.
	/// </remarks>
	/// <param name="builder">The app builder.</param>
	/// <param name="name">The name of the head, <c>&lt;app&gt;-android</c> when omitted.</param>
	/// <returns>A builder for the head.</returns>
	public static IResourceBuilder<UnoAndroidEmulatorResource> AddAndroidEmulator(
		this IResourceBuilder<UnoAppResource> builder,
		[ResourceName] string? name = null)
	{
		ArgumentNullException.ThrowIfNull(builder);

		var head = new UnoAndroidEmulatorResource(name ?? $"{builder.Resource.Name}-android", builder.Resource);

		return builder.ApplicationBuilder.AddHead(head)
			.WithArgs(context =>
			{
				AddRunArguments(head, context.Args);
				context.Args.Add(EmulatorTargetArgument);
			})
			// Started by hand: it needs a running emulator.
			.WithExplicitStart();
	}

	/// <summary>
	/// Sets the target framework the head is built and run for.
	/// </summary>
	/// <param name="builder">The head builder.</param>
	/// <param name="targetFramework">The target framework, for example <c>net10.0-desktop</c>.</param>
	/// <returns>The same head builder.</returns>
	public static IResourceBuilder<T> WithTargetFramework<T>(this IResourceBuilder<T> builder, string targetFramework)
		where T : UnoHeadResource
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrEmpty(targetFramework);

		builder.Resource.TargetFramework = targetFramework;
		return builder;
	}

	/// <summary>
	/// Runs the desktop head with a launch profile of the app. Environment variables of the profile override the ones Aspire injects.
	/// </summary>
	/// <remarks>
	/// Not offered for the WebAssembly head: a profile's application URL overrides the address the AppHost serves it on.
	/// </remarks>
	/// <param name="builder">The head builder.</param>
	/// <param name="profileName">The name of the launch profile.</param>
	/// <returns>The same head builder.</returns>
	public static IResourceBuilder<UnoDesktopResource> WithLaunchProfile(this IResourceBuilder<UnoDesktopResource> builder, string profileName)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrEmpty(profileName);

		builder.Resource.LaunchProfile = profileName;
		return builder;
	}

	private static IResourceBuilder<T> AddHead<T>(this IDistributedApplicationBuilder builder, T head)
		where T : UnoHeadResource
	{
		var resource = builder.AddResource(head)
			// dotnet run without a launch profile does not set it, and Hot Reload needs it.
			.WithEnvironment(ModifiableAssembliesVariable, "debug")
			.ExcludeFromManifest();

		if (builder.ExecutionContext.IsRunMode)
		{
			resource.OnBeforeResourceStarted((head, @event, cancellationToken) => HeadBuild.RunAsync(
				head,
				@event.Services.GetRequiredService<ResourceNotificationService>(),
				@event.Services.GetRequiredService<ResourceLoggerService>().GetLogger(head),
				cancellationToken));
		}

		return resource;
	}

	private static void AddRunArguments(UnoHeadResource head, IList<object> args)
	{
		args.Add("run");
		args.Add("--project");
		args.Add(head.App.ProjectPath);
		args.Add("-f");
		args.Add(head.TargetFramework);
		// Built just before, under the app's build lock.
		args.Add("--no-build");

		if (head.LaunchProfile is null)
		{
			args.Add("--no-launch-profile");
			return;
		}

		args.Add("--launch-profile");
		args.Add(head.LaunchProfile);
	}
}
