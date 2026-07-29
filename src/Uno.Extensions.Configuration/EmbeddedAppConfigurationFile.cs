using System.Runtime.CompilerServices;

namespace Uno.Extensions.Configuration;

/// <summary>
/// Represents an embedded app configuration file that exposes its content as a <see cref="Stream"/>.
/// </summary>
public class EmbeddedAppConfigurationFile
{
	// Cache keyed by the owning assembly rather than a single process-wide array.
	//
	// The previous single static array was first-caller-wins: once populated it was returned for
	// every subsequent AllFiles<T>() call regardless of the type argument. In a downstream host that
	// loads multiple apps into their own collectible AssemblyLoadContexts, that both (1) served the
	// FIRST app's appsettings to every later app (a functional bug) and (2) pinned the first app's
	// Assembly for the process lifetime, preventing its ALC from being collected.
	//
	// A ConditionalWeakTable keyed by Assembly gives each app its own entry and holds the key weakly,
	// so once an app's ALC is unloaded the entry (and the cached Assembly reference) becomes eligible
	// for collection.
	private static readonly ConditionalWeakTable<Assembly, EmbeddedAppConfigurationFile[]> _appConfigurationFiles = new();

	private readonly Assembly _assembly;

	/// <summary>
	/// Initializes a new instance of the <see cref="EmbeddedAppConfigurationFile"/> class.
	/// </summary>
	/// <param name="fileName">
	/// The name of the embedded resource file.
	/// </param>
	/// <param name="assembly">
	/// The assembly that contains the embedded resource file.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="fileName"/> or <paramref name="assembly"/> is <see langword="null"/>.
	/// </exception>
	public EmbeddedAppConfigurationFile(string fileName, Assembly assembly)
	{
		FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
		_assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
	}

	/// <summary>
	/// Gets the name of the embedded resource file.
	/// </summary>
	public string FileName { get; }

	/// <summary>
	/// Gets the content of the embedded resource file as a <see cref="Stream"/>.
	/// </summary>
	/// <returns>
	/// The content of the embedded resource file as a <see cref="Stream"/>.
	/// </returns>
	public Stream? GetContent()
	{
		using (var resourceFileStream = _assembly.GetManifestResourceStream(FileName))
		{
			if (resourceFileStream != null)
			{
				var memoryStream = new MemoryStream();

				resourceFileStream.CopyTo(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);

				return memoryStream;
			}

			return null;
		}
	}

	/// <summary>
	/// Gets all embedded app configuration files from the assembly that contains the specified type.
	/// </summary>
	/// <typeparam name="TApplicationRoot">
	/// The type that will be used to locate an assembly that contains the embedded resource files.
	/// </typeparam>
	/// <returns>
	/// All embedded app configuration files from the assembly that contains the specified type.
	/// </returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Not available for desktop.")]
	public static EmbeddedAppConfigurationFile[] AllFiles<TApplicationRoot>()
		 where TApplicationRoot : class
	{
		var executingAssembly = typeof(TApplicationRoot).Assembly;

		return _appConfigurationFiles.GetValue(
			executingAssembly,
			static assembly => assembly
				.GetManifestResourceNames()
				.Where(fileName => fileName.ToLowerInvariant().Contains(AppConfiguration.Prefix.ToLowerInvariant()))
				.Select(fileName => new EmbeddedAppConfigurationFile(fileName, assembly))
				.ToArray());
	}
}
