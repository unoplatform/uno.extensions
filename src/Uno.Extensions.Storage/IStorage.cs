namespace Uno.Extensions.Storage;

public interface IStorage
{
	Task<string> CreateFolderAsync(string foldername);

	Task<string?> ReadPackageFileAsync(string filename);

	Task<Stream?> OpenPackageFileAsync(string filename);

	Task WriteFileAsync(string filename, string text, bool overwrite);
}
