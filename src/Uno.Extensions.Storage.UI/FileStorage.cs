
using System.Reflection;
#if __IOS__ || MACCATALYST || MACOS
using Foundation;
#endif

namespace Uno.Extensions.Storage;

public class FileStorage : IStorage
{
	private async Task<bool> FileExistsInPackage(string filename)
	{
#if __ANDROID__
		//Look in assets first***
		var context = global::Android.App.Application.Context;
		var assets = context.Assets;
		var normalizedFileName = Path.GetFileNameWithoutExtension(filename).Replace('.', '_').Replace('-','_') + Path.GetExtension(filename);
		var files = new List<string>();
		ScanPackageAssets();
		if (files.Contains(normalizedFileName)) return true;

		var nameArray = filename.ToLower().Split("/")?.ToList();
		var normalizedResName = Path.GetFileNameWithoutExtension(filename).Replace('.', '_').Replace('-','_');
		if(nameArray?.Count() > 1 )
		{
			//Nested resource name
			nameArray[nameArray.Count() -1] = normalizedResName;
			normalizedResName = string.Join("_", nameArray);
		}

		//Look in drawable resources***
		var resources = context.Resources;
		int resId =0;
		resId = resources?.GetIdentifier(normalizedResName, "drawable", context.PackageName) ?? 0;
		if(resId != 0) return true;

		//Look in mipmap resources***
		resId = resources?.GetIdentifier(normalizedResName, "mipmap", context.PackageName) ?? 0;
		if(resId != 0) return true;

		//This method will scan for all the assets within current package
		bool ScanPackageAssets(string rootPath = "")
		{
			try
			{
				var Paths = assets?.List(rootPath);
				if(Paths?.Length >0)
				{
					foreach (var file in Paths)
					{
						string path = string.IsNullOrEmpty(rootPath)? file: rootPath+"/"+file;
						if(!ScanPackageAssets(path)) return false;
						else if(path.Contains('.')) files.Add(Path.GetFileNameWithoutExtension(path) + Path.GetExtension(path));
					}
				}
			}
			catch{return false;}
			return true;
		}

		return false;
#elif __IOS__ || MACCATALYST || MACOS
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
