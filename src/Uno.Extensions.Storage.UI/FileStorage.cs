using System.Reflection;
#if __IOS__ || MACCATALYST || MACOS
using Foundation;
#endif

namespace Uno.Extensions.Storage;

public class FileStorage : IStorage
{
	private async Task<bool> FileExistsInPackage(string filename)
	{
#if __IOS__ || MACCATALYST || MACOS
		var directoryName = global::System.IO.Path.GetDirectoryName(filename) + string.Empty;
		var fileName = global::System.IO.Path.GetFileNameWithoutExtension(filename);
		var fileExtension = global::System.IO.Path.GetExtension(filename);

		var resourcePathname = NSBundle.MainBundle.PathForResource(global::System.IO.Path.Combine(directoryName, fileName), fileExtension.Substring(1));

		return resourcePathname != null;
#elif WINDOWS
		var executingPath = Assembly.GetExecutingAssembly().Location;
		if (!string.IsNullOrWhiteSpace(executingPath))
		{
			var path = Path.GetDirectoryName(executingPath);
			if (path is not null &&
				!string.IsNullOrWhiteSpace(path))
			{
				var fullPath = Path.Combine(path, filename);
				return File.Exists(fullPath);
			}
		}
		return true;
#else
		return true;
#endif

	}

	public async Task<string> CreateFolderAsync(string foldername)
	{
		var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
		var folder = await localFolder.CreateFolderAsync(foldername, CreationCollisionOption.OpenIfExists);
		return folder.Path;
	}

	public async Task<string?> ReadPackageFileAsync(string filename)
	{
		try
		{
#if __ANDROID__
				var assets = global::Android.App.Application.Context.Assets;
				var inputStream = assets?.Open(filename);
				var content = inputStream?.ReadToEnd();
				inputStream?.Close();
				return content;
#else

			if (!await FileExistsInPackage(filename))
			{
				return default;
			}

			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
			if (File.Exists(storageFile.Path))
			{
				var settings = File.ReadAllText(storageFile.Path);
				return settings;
			}
#endif
		}
		catch
		{
			
		}

		return default;
	}

	public async Task<Stream?> OpenPackageFileAsync(string filename)
	{

		try
		{
#if __ANDROID__
			var assets = global::Android.App.Application.Context.Assets;
			var inputStream = assets?.Open(filename);
			return inputStream;
#else
			if (!await FileExistsInPackage(filename))
			{
				return default;
			}

			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
			var stream = await storageFile.OpenStreamForReadAsync();
			return stream;
#endif
		}
		catch
		{
		}

		return default;
	}

	public async Task WriteFileAsync(string filename, string text, bool overwrite)
	{
		if (!File.Exists(filename) || overwrite)
		{
			File.WriteAllText(filename, text);
		}
	}
}
