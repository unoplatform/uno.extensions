using System.IO;
using System.Threading.Tasks;

namespace Uno.Extensions.Storage;

public interface IStorage
{
	Task<string> CreateLocalFolderAsync(string foldername);

	Task<string?> ReadFromApplicationFileAsync(string filename);

	Task<Stream> OpenApplicationFileAsync(string filename);

	Task WriteToFileAsync(string filename, string text);
}
