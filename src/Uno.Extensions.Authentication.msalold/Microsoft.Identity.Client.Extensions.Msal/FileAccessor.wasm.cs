

namespace Microsoft.Identity.Client.Extensions.Msal.Wasm;

internal class FileAccessor : ICacheAccessor
{
	public static readonly byte[] DummyData = Encoding.UTF8.GetBytes("{}");

	private readonly string _cacheFilePath;

	private readonly ILogger _logger;

	internal FileAccessor(ILogger logger, string cacheFilePath)
	{
		_logger = logger;
		_cacheFilePath = cacheFilePath;
	}

	public void Clear()
	{
		_logger.LogInformation("Deleting cache file");
		File.Delete(_cacheFilePath);
	}

	public ICacheAccessor CreateForPersistenceValidation(ILogger logger)
	{
		return new FileAccessor(logger, _cacheFilePath + ".test");//, _setOwnerOnlyPermission, _logger);
	}

	public byte[]? Read()
	{
		_logger.LogInformation("Reading from file");
		byte[]? fileData = null;
		bool flag = File.Exists(_cacheFilePath);
		_logger.LogInformation($"Cache file exists? '{flag}'");
		if (flag)
		{
			fileData = File.ReadAllBytes(_cacheFilePath);
			_logger.LogInformation($"Read '{fileData.Length}' bytes from the file");
		}
		return fileData;
	}
	public void Write(byte[] data)
	{
		_logger.LogInformation("Writing cache file");
		EnsureParentDirectoryExists(_cacheFilePath);
		File.WriteAllBytes(_cacheFilePath, data);
	}


	private void EnsureParentDirectoryExists(string filePath)
	{
		if (!Directory.Exists(Path.GetDirectoryName(filePath)))
		{
			string directoryName = Path.GetDirectoryName(filePath)!;
			_logger.LogInformation("Creating directory '" + directoryName + "'");
			Directory.CreateDirectory(directoryName);
		}
	}
}



