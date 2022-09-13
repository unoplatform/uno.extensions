namespace Uno.Extensions.Storage;

public interface IStorage
{
	Task<string> CreateLocalFolderAsync(string foldername);

	Task<string?> ReadFileAsync(string filename);

	Task<Stream> OpenFileAsync(string filename);

	Task WriteFileAsync(string filename, string text);
}
