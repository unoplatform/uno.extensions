

namespace Microsoft.Identity.Client.Extensions.Msal.Wasm;

public class Storage
{
	private readonly ILogger _logger;

	internal const string PersistenceValidationDummyData = "msal_persistence_test";

	internal ICacheAccessor CacheAccessor { get; }

	/// <summary>
	/// The storage creation properties used to create this storage
	/// </summary>
	internal StorageCreationProperties StorageCreationProperties { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Microsoft.Identity.Client.Extensions.Msal.Storage" /> class.
	/// The actual cache reading and writing is OS specific:
	/// <list type="bullet">
	/// <item>
	///     <term>Windows</term>
	///     <description>DPAPI encrypted file on behalf of the user. </description>
	/// </item>
	/// <item>
	///     <term>Mac</term>
	///     <description>Cache is stored in KeyChain.  </description>
	/// </item>
	/// <item>
	///     <term>Linux</term>
	///     <description>Cache is stored in Gnome KeyRing - https://developer.gnome.org/libsecret/0.18/  </description>
	/// </item>
	/// </list>
	/// </summary>
	/// <param name="creationProperties">Properties for creating the cache storage on disk</param>
	/// <param name="logger">logger</param>
	/// <returns></returns>
	public static Storage Create(ILogger logger, StorageCreationProperties creationProperties)
	{
		var cacheAccessor = new FileAccessor(logger, creationProperties.CacheFilePath);
		return new Storage(logger, creationProperties, cacheAccessor);
	}

	internal Storage(ILogger logger, StorageCreationProperties creationProperties, ICacheAccessor cacheAccessor)
	{
		StorageCreationProperties = creationProperties;
		_logger = logger;
		CacheAccessor = cacheAccessor;
		_logger.LogInformation("Initialized 'Storage'");
	}

	/// <summary>
	/// Read and unprotect cache data
	/// </summary>
	/// <returns>Unprotected cache data</returns>
	public byte[]? ReadData()
	{
		byte[]? array;
		try
		{
			_logger.LogInformation("Reading Data");
			array = CacheAccessor.Read();
			_logger.LogInformation($"Got '{((array != null) ? array.Length : 0)}' bytes from file storage");
		}
		catch (Exception arg)
		{
			_logger.LogError(string.Format("An exception was encountered while reading data from the {0} : {1}", "Storage", arg));
			throw;
		}
		return array;
	}

	/// <summary>
	/// Protect and write cache data to file. It overrides existing data.
	/// </summary>
	/// <param name="data">Cache data</param>
	public void WriteData(byte[] data)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		try
		{
			_logger.LogInformation($"Got '{data.Length}' bytes to write to storage");
			CacheAccessor.Write(data);
		}
		catch (Exception arg)
		{
			_logger.LogError(string.Format("An exception was encountered while writing data to {0} : {1}", "Storage", arg));
			throw;
		}
	}

	/// <summary>
	/// Delete cache file
	/// </summary>
	/// <param name="ignoreExceptions">Throw on exceptions</param>
	public void Clear(bool ignoreExceptions = false)
	{
		try
		{
			_logger.LogInformation("Clearing the cache file");
			CacheAccessor.Clear();
		}
		catch (Exception arg)
		{
			_logger.LogError(string.Format("An exception was encountered while clearing data from {0} : {1}", "Storage", arg));
			if (!ignoreExceptions)
			{
				throw;
			}
		}
	}

	/// <summary>
	/// Tries to write -&gt; read -&gt; clear a secret from the underlying persistence mechanism
	/// </summary>
	public void VerifyPersistence()
	{
		ICacheAccessor cacheAccessor = CacheAccessor.CreateForPersistenceValidation(_logger);
		try
		{
			_logger.LogInformation("[Verify Persistence] Writing Data ");
			cacheAccessor.Write(Encoding.UTF8.GetBytes("msal_persistence_test"));
			_logger.LogInformation("[Verify Persistence] Reading Data ");
			var array = cacheAccessor.Read();
			if (array == null || array.Length == 0)
			{
				throw new MsalCachePersistenceException("Persistence check failed. Data was written but it could not be read. Possible cause: on Linux, LibSecret is installed but D-Bus isn't running because it cannot be started over SSH.");
			}
			string @string = Encoding.UTF8.GetString(array);
			if (!string.Equals("msal_persistence_test", @string, StringComparison.Ordinal))
			{
				throw new MsalCachePersistenceException("Persistence check failed. Data written msal_persistence_test is different from data read " + @string);
			}
		}
		catch (Exception ex)
		{
			throw new MsalCachePersistenceException("Persistence check failed. Inspect inner exception for details", ex);
		}
		finally
		{
			try
			{
				_logger.LogInformation("[Verify Persistence] Clearing data");
				cacheAccessor.Clear();
			}
			catch (Exception ex3)
			{
				_logger.LogError("[Verify Persistence] Could not clear the test data: " + ex3);
			}
		}
	}
}



