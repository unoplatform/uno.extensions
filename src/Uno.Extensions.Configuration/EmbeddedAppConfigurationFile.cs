

namespace Uno.Extensions.Configuration;

public class EmbeddedAppConfigurationFile
{

	private static EmbeddedAppConfigurationFile[]? _appConfigurationFiles;

	private readonly Assembly _assembly;

	public EmbeddedAppConfigurationFile(string fileName, Assembly assembly)
	{
		FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
		_assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
	}

	public string FileName { get; }

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

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Not available for desktop.")]
	public static EmbeddedAppConfigurationFile[] AllFiles<TApplicationRoot>()
		 where TApplicationRoot : class
	{
		if (_appConfigurationFiles is null)
		{
			var executingAssembly = typeof(TApplicationRoot).Assembly;

			_appConfigurationFiles = executingAssembly
				.GetManifestResourceNames()
				.Where(fileName => fileName.ToLowerInvariant().Contains(AppConfiguration.Prefix.ToLowerInvariant()))
				.Select(fileName => new EmbeddedAppConfigurationFile(fileName, executingAssembly))
				.ToArray();
		}

		return _appConfigurationFiles;
	}
}
