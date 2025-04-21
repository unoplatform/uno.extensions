using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text;
namespace Uno.Extensions.Storage;

internal record FileStorage(ILogger<FileStorage> Logger, IDataFolderProvider DataFolderProvider) : IStorage
{
	private Task<bool> FileExistsInPackage(string fileName) => Uno.UI.Toolkit.StorageFileHelper.ExistsInPackage(fileName);

	public async Task<string?> CreateFolderAsync(string foldername)
	{
		var path = DataFolderProvider.AppDataPath;
		if (path is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage("No application data path, so unable to create folder");
			}
			return default;
		}

		var localFolder = await StorageFolder.GetFolderFromPathAsync(path);
		var folder = await localFolder.CreateFolderAsync(foldername, CreationCollisionOption.OpenIfExists);
		return folder.Path;
	}

	public async Task<string?> ReadPackageFileAsync(string filename)
	{
		try
		{
			if (!await FileExistsInPackage(filename))
			{
				if (Logger.IsEnabled(LogLevel.Information))
				{
					Logger.LogInformationMessage($"File '{filename}' does not exist in package");
				}
				return default;
			}

#if __WINDOWS__
			if (!PlatformHelper.IsAppPackaged)
			{
				var file = System.IO.Path.Combine(
								 System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location ?? string.Empty) ?? string.Empty,
								 filename);
				return File.ReadAllText(file);
			}
#endif

			var fileUri = new Uri($"ms-appx:///{filename}");
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Reading file '{fileUri}'");
			}
			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(fileUri);
			if (File.Exists(storageFile.Path))
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage($"Reading file with path '{storageFile.Path}' that does exist");
				}
				var settings = File.ReadAllText(storageFile.Path);
				return settings;
			}

			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"File doesn't exist with path '{storageFile.Path}'");
			}
			return default;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformationMessage($"Unable to read file '{filename}' due to exception {ex.Message}");
			}
			return default;
		}

	}

	/// <inheritdoc/>
	public async Task<ImmutableList<string>?> ReadPackageFileAsync(string filename,List<(int Start, int End)> lineRanges)
	{
		try
		{
			if (!await FileExistsInPackage(filename))
			{
				if (Logger.IsEnabled(LogLevel.Information))
				{
					Logger.LogInformationMessage($"File '{filename}' does not exist in package");
				}
				return default;
			}

#if __WINDOWS__
			if (!PlatformHelper.IsAppPackaged)
			{
				var file = System.IO.Path.Combine(
								 System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location ?? string.Empty) ?? string.Empty,
								 filename);

				 string[] fileContent = File.ReadAllLines(file);
				 if (fileContent.Length == 0)
		         {
					 if (Logger.IsEnabled(LogLevel.Warning))
					 {
						 Logger.LogWarningMessage($"File '{filename}' is empty");
					 }
					 return default;
				 }
				 return GetSelectedLines(fileContent, lineRanges);
			}
#endif

			var fileUri = new Uri($"ms-appx:///{filename}");
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Reading file '{fileUri}'");
			}
			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(fileUri);
			if (File.Exists(storageFile.Path))
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage($"Reading file with path '{storageFile.Path}' that does exist");
				}
				
				string[] fileContent = File.ReadAllLines(storageFile.Path);

				if (fileContent.Length == 0)
				{
					if (Logger.IsEnabled(LogLevel.Warning))
					{
						Logger.LogWarningMessage($"File '{filename}' is empty");
					}
					return default;
				}

				return GetSelectedLines(fileContent, lineRanges);
			}

			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"File doesn't exist with path '{storageFile.Path}'");
			}
			return default;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformationMessage($"Unable to read file '{filename}' due to exception {ex.Message}");
			}
			return default;
		}

	}

	private ImmutableList<string> GetSelectedLines(string[] fileContent, List<(int Start, int End)> lineRanges)
	{
		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"Selecting lines '{lineRanges}'");
		}

		var selectedLines = new List<string>();
		foreach (var (Start, End) in lineRanges)
		{
			var start = Math.Clamp(Start, 0, fileContent.Length);
			var end = Math.Clamp(End, start, fileContent.Length);
			selectedLines.AddRange(fileContent[start..end]);
		}
		if (Logger.IsEnabled(LogLevel.Trace))
		{
			if(selectedLines.Count == 0)
			{
				Logger.LogTraceMessage($"No lines matched {lineRanges}");
			}
			else
			{
				Logger.LogTraceMessage($"Selected lines (Count: {selectedLines.Count}) successfull");
			}
			
		}
		return selectedLines.ToImmutableList();
	}

	public async Task<Stream?> OpenPackageFileAsync(string filename)
	{
		try
		{
			if (!await FileExistsInPackage(filename))
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarningMessage($"File '{filename}' does not exist in package");
				}
				return default;
			}

#if __WINDOWS__
			if (!PlatformHelper.IsAppPackaged)
			{
				var file = System.IO.Path.Combine(
								 System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location ?? string.Empty) ?? string.Empty,
								 filename);
				return System.IO.File.OpenRead(file);
			}
#endif

			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
			var stream = await storageFile.OpenStreamForReadAsync();
			return stream;
		}
		catch
		{
			return default;
		}
	}

	public async Task WriteFileAsync(string filename, string text, bool overwrite)
	{
		try
		{
			if (!File.Exists(filename) || overwrite)
			{
				File.WriteAllText(filename, text);
			}
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Unable to write file '{filename}' due to exception {ex.Message}");
			}
			throw;
		}
	}

}
