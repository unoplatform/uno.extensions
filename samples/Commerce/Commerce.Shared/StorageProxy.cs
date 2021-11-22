using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions.Storage;
using Windows.Storage;

namespace Uno.Extensions.Hosting;

public class StorageProxy : IStorageProxy
{
	public async Task<string> CreateLocalFolder(string foldername)
	{
		var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
		var folder = await localFolder.CreateFolderAsync(foldername, CreationCollisionOption.OpenIfExists);
		return folder.Path;
	}

	public async Task<string?> ReadFromApplicationFile(string filename)
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

	public async Task<Stream> OpenApplicationFile(string filename)
	{
		var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{filename}"));
		var stream = await storageFile.OpenStreamForReadAsync();
		return stream;
	}

	public async Task WriteToFile(string filename, string text)
	{
		//var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
		//var settingsFile = await localFolder.CreateFileAsync($"{filename}", CreationCollisionOption.OpenIfExists);
		File.WriteAllText(filename, text);
	}

}
