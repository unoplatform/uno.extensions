
using System.Reflection;
using Uno.UI.Toolkit;
#if __ANDROID__
using Android.Content.Res;
#elif __IOS__ || MACCATALYST || MACOS
using Foundation;
#endif

namespace Uno.Extensions.Storage;

public class FileStorage : IStorage
{
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
			if (!await StorageFileHelper.ExistsInPackage(filename))
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
		try
		{
			if (!await StorageFileHelper.ExistsInPackage(filename))
			{
				return default;
			}

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
		if (!File.Exists(filename) || overwrite)
		{
			File.WriteAllText(filename, text);
		}
	}

}
