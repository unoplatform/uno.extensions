namespace Uno.Extensions.Hosting;

public class StorageProxy : IStorage
{
	public async Task<string> CreateLocalFolderAsync(string foldername)
	{
		var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
		var folder = await localFolder.CreateFolderAsync(foldername, CreationCollisionOption.OpenIfExists);
		return folder.Path;
	}

	public async Task<string?> ReadFromApplicationFileAsync(string filename)
	{
		try
		{
			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
			if (File.Exists(storageFile.Path))
			{
				var settings = File.ReadAllText(storageFile.Path);
				return settings;
			}

			return null;
		}
		catch
		{
			return null;
		}

	}

	public async Task<Stream> OpenApplicationFileAsync(string filename)
	{
		var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
		var stream = await storageFile.OpenStreamForReadAsync();
		return stream;
	}

	public async Task WriteToFileAsync(string filename, string text)
	{
		//var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
		//var settingsFile = await localFolder.CreateFileAsync($"{filename}", CreationCollisionOption.OpenIfExists);
		File.WriteAllText(filename, text);
	}

}
