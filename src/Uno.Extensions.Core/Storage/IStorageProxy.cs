using System.IO;
using System.Threading.Tasks;

namespace Uno.Extensions.Storage;

public interface IStorageProxy
{
	Task<string> CreateLocalFolder(string foldername);

	Task<string?> ReadFromApplicationFile(string filename);

	Task<Stream> OpenApplicationFile(string filename);

	Task WriteToFile(string filename, string text);
}
