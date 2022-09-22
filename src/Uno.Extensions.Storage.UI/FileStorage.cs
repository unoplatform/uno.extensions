
namespace Uno.Extensions.Storage;

public class FileStorage : IStorage
{
	private async Task<bool> FileExistsInPackage(string filename)
	{
#if __ANDROID__
		var assets = global::Android.App.Application.Context.Assets;
		var files = assets?.List("");
		filename = Path.GetFileNameWithoutExtension(filename).Replace('.', '_') + Path.GetExtension(filename);
		return files?.Contains(filename)??false;
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
			if(!await FileExistsInPackage(filename))
			{
				return default;
			}
			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
			if (File.Exists(storageFile.Path))
			{
				var settings = File.ReadAllText(storageFile.Path);
				return settings;
			}

			return default;
		}
		catch
		{
			return default;
		}

	}

	public async Task<Stream?> OpenPackageFileAsync(string filename)
	{
		if (!await FileExistsInPackage(filename))
		{
			return default;
		}

		var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
		var stream = await storageFile.OpenStreamForReadAsync();
		return stream;
	}

	public async Task WriteFileAsync(string filename, string text, bool overwrite)
	{
		if (!File.Exists(filename) || overwrite)
		{
			File.WriteAllText(filename, text);
		}
	}

}
