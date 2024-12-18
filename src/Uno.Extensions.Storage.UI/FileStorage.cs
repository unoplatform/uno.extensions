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
