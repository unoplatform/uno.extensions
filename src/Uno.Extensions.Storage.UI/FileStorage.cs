﻿namespace Uno.Extensions.Storage;

internal record FileStorage(IDataFolderProvider DataFolderProvider) : IStorage
{
	private Task<bool> FileExistsInPackage(string fileName) => Uno.UI.Toolkit.StorageFileHelper.ExistsInPackage(fileName);

	public async Task<string?> CreateFolderAsync(string foldername)
	{
		var path = DataFolderProvider.AppDataPath;
		if(path is null)
		{
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
			if (!await FileExistsInPackage(filename))
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
