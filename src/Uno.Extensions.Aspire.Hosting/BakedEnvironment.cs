using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Writes the environment of a head that has no process environment into a file the app's build reads.
/// </summary>
/// <remarks>
/// The build logic of Uno.Extensions.Aspire reads the file from the app's obj folder, so every Debug build of the app finds it.
/// See specs/027-aspire-support/spec.md, "Baking the environment".
/// </remarks>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
internal static partial class BakedEnvironment
{
	internal const string ModifiableAssembliesVariable = "DOTNET_MODIFIABLE_ASSEMBLIES";
	internal const string ServerUrlsVariable = "ASPNETCORE_URLS";
	internal const string EnvironmentVariable = "DOTNET_ENVIRONMENT";

	// Part of the file name. Uno.Extensions.Aspire warns about a file of another version instead of reading it.
	internal const string FormatVersion = "v1";

	// The emulator's address for the host machine's loopback interface.
	internal const string EmulatorHostAddress = "10.0.2.2";

	private const string ServicePrefix = "services__";
	private const string ValuesTarget = "GetEnvironment";
	private const string TemporarySuffix = ".tmp";

	// For the head's process (the dev server, dotnet run), not written into the app.
	private static readonly HashSet<string> ProcessOnlyNames = new(StringComparer.Ordinal) { ModifiableAssembliesVariable, ServerUrlsVariable, EnvironmentVariable };

	// Security: a baked value is readable by anyone with the app. Only service endpoints and opted-in names pass.
	internal static BakePlan Plan(IEnumerable<(string Name, object? Unprocessed, string Processed)> environment, IReadOnlySet<string> optedInNames)
	{
		var variables = new SortedDictionary<string, string>(StringComparer.Ordinal);
		var leftOutServices = new List<string>();
		var leftOut = new List<string>();
		var secretOptIns = new List<string>();

		foreach (var (name, unprocessed, processed) in environment)
		{
			if (optedInNames.Contains(name))
			{
				variables[name] = processed;
				if (ContainsSecret(unprocessed))
				{
					secretOptIns.Add(name);
				}
			}
			else if (name.StartsWith(ServicePrefix, StringComparison.Ordinal))
			{
				if (IsEndpointOnly(unprocessed))
				{
					variables[name] = processed;
				}
				else
				{
					leftOutServices.Add(name);
				}
			}
			else if (!ProcessOnlyNames.Contains(name))
			{
				leftOut.Add(name);
			}
		}

		var missingOptIns = optedInNames.Where(name => !variables.ContainsKey(name)).Order(StringComparer.Ordinal).ToList();
		return new BakePlan(variables, leftOutServices, leftOut, missingOptIns, secretOptIns);
	}

	/// <summary>
	/// The browser takes the values as they are.
	/// </summary>
	internal static BakePlan ForWebAssembly(BakePlan plan) => plan;

	/// <summary>
	/// What an Android emulator can use. A service address on the host's loopback gets the emulator's address for the host instead,
	/// and is left out over <c>https</c>, whose development certificate the emulator does not trust. Names are in upper case,
	/// because the Android runtime drops lowercase ones; configuration keys are case-insensitive.
	/// </summary>
	/// <remarks>
	/// Every <c>services__</c> variable is treated as a service address, whether it passed the allow-list or was opted in.
	/// </remarks>
	internal static BakePlan ForAndroidEmulator(BakePlan plan)
	{
		var variables = new SortedDictionary<string, string>(StringComparer.Ordinal);
		var leftOutUntrusted = new List<string>();
		foreach (var (name, value) in plan.Variables)
		{
			var baked = value;
			if (name.StartsWith(ServicePrefix, StringComparison.Ordinal)
				&& Uri.TryCreate(value, UriKind.Absolute, out var uri)
				&& uri.IsLoopback)
			{
				if (uri.Scheme == Uri.UriSchemeHttps)
				{
					leftOutUntrusted.Add(name);
					continue;
				}

				baked = ToEmulatorHost(uri, value);
			}

			if (!variables.TryAdd(name.ToUpperInvariant(), baked))
			{
				throw new DistributedApplicationException($"The environment variable '{name}' cannot be written into the Android app: another one differs from it only by case.");
			}
		}

		return plan with
		{
			Variables = variables,
			LeftOutUntrusted = leftOutUntrusted,
			UnreachableServices = ServicesWithoutAddress(leftOutUntrusted, variables.Keys),
		};
	}

	/// <summary>
	/// Where the head's values are written. Uno.Extensions.Aspire reads the same path and format: keep the two in step,
	/// and change <see cref="FormatVersion"/> with the format so a mismatched client warns instead of reading it.
	/// </summary>
	internal static string ValuesPath(UnoHeadResource head) =>
		Path.Join(head.App.ProjectDirectory, "obj", "uno-aspire", $"{head.TargetFramework}.{FormatVersion}.values.proj");

	/// <summary>
	/// The project that returns the values, called by the app's build.
	/// </summary>
	internal static string CreateValues(string headName, IReadOnlyDictionary<string, string> variables)
	{
		var items = new XElement("ItemGroup");
		foreach (var (name, value) in variables)
		{
			Validate(headName, name, value);
			items.Add(new XElement("_UnoAspireEnvironment", new XAttribute("Include", name), new XAttribute("Value", EscapeForMSBuild(value))));
		}

		return new XElement(
			"Project",
			items,
			new XElement("Target", new XAttribute("Name", ValuesTarget), new XAttribute("Returns", "@(_UnoAspireEnvironment)"))).ToString();
	}

	// Leaves an unchanged file untouched, and never leaves a half-written one for a build to read.
	internal static void WriteIfChanged(string headName, string path, string content)
	{
		try
		{
			if (File.Exists(path) && File.ReadAllText(path) == content)
			{
				return;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new ArgumentException("The path has no directory.", nameof(path)));
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			throw new DistributedApplicationException($"The environment of '{headName}' could not be written to '{path}': {exception.Message}", exception);
		}

		// Unique per write: two heads of one app may write the same file at once.
		var temporaryPath = $"{path}.{Path.GetRandomFileName()}{TemporarySuffix}";
		try
		{
			File.WriteAllText(temporaryPath, content, Encoding.UTF8);
			File.Move(temporaryPath, path, overwrite: true);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			// A temporary file left here is removed with the values, see Delete.
			throw new DistributedApplicationException($"The environment of '{headName}' could not be written to '{path}': {exception.Message}", exception);
		}
	}

	// A file left after the AppHost stops would put a dead address into the app's next build.
	internal static void Delete(UnoHeadResource head, ILogger logger)
	{
		var path = ValuesPath(head);
		var folder = Path.GetDirectoryName(path);
		if (folder is null || !Directory.Exists(folder))
		{
			return;
		}

		try
		{
			File.Delete(path);
			foreach (var temporaryPath in Directory.EnumerateFiles(folder, $"{Path.GetFileName(path)}.*{TemporarySuffix}"))
			{
				File.Delete(temporaryPath);
			}
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			logger.LogWarning(exception, "The environment of '{Head}' could not be removed from '{Path}'. Delete it, or the app's next build carries its values.", head.Name, path);
		}
	}

	private static string ToEmulatorHost(Uri uri, string value)
	{
		var rewritten = new UriBuilder(uri) { Host = EmulatorHostAddress }.Uri.AbsoluteUri;
		return value.EndsWith('/') ? rewritten : rewritten.TrimEnd('/');
	}

	// "services__api__https__0" names the service "api".
	private static List<string> ServicesWithoutAddress(IEnumerable<string> leftOut, IEnumerable<string> bakedNames) =>
		leftOut
			.Select(name => name.Split("__")[1])
			.Distinct()
			.Where(service =>
			{
				var prefix = $"{ServicePrefix}{service}__".ToUpperInvariant();
				return !bakedNames.Any(baked => baked.StartsWith(prefix, StringComparison.Ordinal));
			})
			.ToList();

	private static bool IsEndpointOnly(object? value) => value switch
	{
		EndpointReference or EndpointReferenceExpression => true,
		ReferenceExpression { IsConditional: false, ValueProviders: { Count: > 0 } providers } => providers.All(IsEndpointOnly),
		_ => false,
	};

	private static bool ContainsSecret(object? value) => value switch
	{
		ParameterResource parameter => parameter.Secret,
		ConnectionStringReference or IResourceWithConnectionString => true,
		ReferenceExpression { IsConditional: true } conditional => ContainsSecret(conditional.WhenTrue) || ContainsSecret(conditional.WhenFalse),
		ReferenceExpression expression => expression.ValueProviders.Any(ContainsSecret),
		_ => false,
	};

	private static void Validate(string headName, string name, string value)
	{
		if (!VariableName().IsMatch(name))
		{
			throw new DistributedApplicationException($"The environment variable '{name}' of '{headName}' cannot be written into the app: its name is not an identifier.");
		}

		if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
		{
			throw new DistributedApplicationException($"The environment variable '{name}' of '{headName}' cannot be written into the app: its value has a line break.");
		}
	}

	private static string EscapeForMSBuild(string value)
	{
		var escaped = new StringBuilder(value.Length);
		foreach (var character in value)
		{
			if ("%*?@$();'".Contains(character))
			{
				escaped.Append('%').Append(((int)character).ToString("X2"));
			}
			else
			{
				escaped.Append(character);
			}
		}

		return escaped.ToString();
	}

	[GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
	private static partial Regex VariableName();
}
