

namespace Microsoft.Identity.Client.Extensions.Msal.Wasm;

/// <summary>
/// Helper to create the token cache
/// </summary>
internal class MsalCacheHelper
{
	/// <summary>
	/// A lock object for serialization
	/// </summary>
	private readonly object _lockObject = new object();

	/// <summary>
	/// Properties used to create storage on disk.
	/// </summary>
	private readonly StorageCreationProperties _storageCreationProperties;


	/// <summary>
	/// Storage that handles the storing of the adal cache file on disk. 
	/// </summary>
	private Storage CacheStore { get; }

	/// <summary>
	/// Logger to log events to.
	/// </summary>
	private readonly ILogger _logger;


	/// <summary>
	/// Creates a new instance of this class.
	/// </summary>
	/// <param name="storageCreationProperties">Properties to use when creating storage on disk.</param>
	/// <param name="logger">Passing null uses a default logger</param>
	/// <param name="knownAccountIds">The set of known accounts</param>
	/// <param name="cacheWatcher">Watcher for the cache file, to enable sending updated events</param>
	private MsalCacheHelper(
		ILogger logger,
		StorageCreationProperties storageCreationProperties)
	{
		_logger = logger;
		_storageCreationProperties = storageCreationProperties;
		CacheStore = Storage.Create(logger, _storageCreationProperties);
	}


	/// <summary>
	/// Creates a new instance of <see cref="MsalCacheHelper"/>. To configure MSAL to use this cache persistence, call <see cref="RegisterCache(ITokenCache)"/>
	/// </summary>
	/// <param name="logger">Passing null uses the default TraceSource logger. See https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Logging for details.</param>
	/// <param name="storageCreationProperties">Properties to use when creating storage on disk.</param>
	/// <returns>A new instance of <see cref="MsalCacheHelper"/>.</returns>
	public static async Task<MsalCacheHelper> CreateAsync(ILogger logger, StorageCreationProperties storageCreationProperties)
	{
		if (storageCreationProperties is null)
		{
			throw new ArgumentNullException(nameof(storageCreationProperties));
		}


		return new MsalCacheHelper(logger, storageCreationProperties);
	}

	/// <summary>
	/// Registers a token cache to synchronize with the persistent storage.
	/// </summary>
	/// <param name="tokenCache">The application token cache, typically referenced as <see cref="IClientApplicationBase.UserTokenCache"/></param>
	/// <remarks>Call <see cref="UnregisterCache(ITokenCache)"/> to have the given token cache stop syncronizing.</remarks>
	public void RegisterCache(Microsoft.Identity.Client.ITokenCache tokenCache)
	{
		if (tokenCache == null)
		{
			throw new ArgumentNullException(nameof(tokenCache));
		}

		_logger.LogInformation($"Registering token cache with on disk storage");

		// If the token cache was already registered, this operation does nothing
		tokenCache.SetBeforeAccess(BeforeAccessNotification);
		tokenCache.SetAfterAccess(AfterAccessNotification);

		_logger.LogInformation($"Done initializing");
	}

	/// <summary>
	/// Unregisters a token cache so it no longer synchronizes with on disk storage.
	/// </summary>
	public void UnregisterCache(Microsoft.Identity.Client.ITokenCache tokenCache)
	{
		if (tokenCache == null)
		{
			throw new ArgumentNullException(nameof(tokenCache));
		}

		tokenCache.SetBeforeAccess(null);
		tokenCache.SetAfterAccess(null);
	}

	/// <summary>
	/// Clears the token store. Equivalent to a delete operation on the persistence layer (file delete). 
	/// </summary>
	/// <remarks>
	/// Apps should use MSAL's RemoveAccount to delete accounts, which is guaranteed to remove confidential information about that account. The token
	/// cache also contains metadata required for MSAL to operate, degrading the experience and perf when deleted.
	/// </remarks>
	[Obsolete(
		"Applications should not delete the entire cache to log out all users. Instead, call app.RemoveAsync(IAccount) for each account in the cache. ",
		false)]
	public void Clear()
	{
		CacheStore.Clear(ignoreExceptions: true);
	}

	/// <summary>
	/// Extracts the token cache data from the persistent store 
	/// </summary>
	/// <returns>an UTF-8 byte array of the unencrypted token cache</returns>
	/// <remarks>This method should be used with care. The data returned is unencrypted.</remarks>
	public byte[]? LoadUnencryptedTokenCache()
	{
		return CacheStore.ReadData();
	}

	/// <summary>
	/// Saves an unencrypted, UTF-8 encoded byte array representing an MSAL token cache.
	/// The save operation will persist the data in a secure location, as configured in <see cref="StorageCreationProperties"/>
	/// </summary>
	public void SaveUnencryptedTokenCache(byte[] tokenCache)
	{
		CacheStore.WriteData(tokenCache);
	}


	/// <summary>
	/// Before cache access
	/// </summary>
	/// <param name="args">Callback parameters from MSAL</param>
	internal void BeforeAccessNotification(TokenCacheNotificationArgs args)
	{
		_logger.LogInformation($"Before access");

		_logger.LogInformation($"Acquiring lock for token cache");


		_logger.LogInformation($"Before access, the store has changed");
		byte[]? cachedStoreData = null;
		try
		{
			cachedStoreData = CacheStore.ReadData();
		}
		catch (Exception)
		{
			_logger.LogError($"Could not read the token cache. Ignoring. See previous error message.");
			return;

		}
		_logger.LogInformation($"Read '{cachedStoreData?.Length}' bytes from storage");

		lock (_lockObject)
		{
			try
			{
				_logger.LogInformation($"Deserializing the store");
				args.TokenCache.DeserializeMsalV3(cachedStoreData, shouldClearExistingCache: true);
			}
			catch (Exception e)
			{
				_logger.LogError($"An exception was encountered while deserializing the {nameof(MsalCacheHelper)} : {e}");
				_logger.LogError($"No data found in the store, clearing the cache in memory.");

				// Clear the memory cache without taking the lock over again
				CacheStore.Clear(ignoreExceptions: true);
				throw;
			}
		}
	}

	/// <summary>
	/// After cache access
	/// </summary>
	/// <param name="args">Callback parameters from MSAL</param>
	internal void AfterAccessNotification(TokenCacheNotificationArgs args)
	{
		try
		{
			_logger.LogInformation($"After access");
			byte[]? data = null;
			// if the access operation resulted in a cache update
			if (args.HasStateChanged)
			{
				_logger.LogInformation($"After access, cache in memory HasChanged");
				try
				{
					data = args.TokenCache.SerializeMsalV3();
				}
				catch (Exception e)
				{
					_logger.LogInformation($"An exception was encountered while serializing the {nameof(MsalCacheHelper)} : {e}");
					_logger.LogInformation($"No data found in the store, clearing the cache in memory.");

					// The cache is corrupt clear it out
					CacheStore.Clear(ignoreExceptions: true);
					throw;
				}

				if (data != null)
				{
					_logger.LogInformation($"Serializing '{data.Length}' bytes");
					try
					{
						CacheStore.WriteData(data);
					}
					catch (Exception)
					{
						_logger.LogError($"Could not write the token cache. Ignoring. See previous error message.");
					}
				}
			}
		}
		finally
		{
			ReleaseFileLock();
		}
	}

	private void ReleaseFileLock()
	{
		_logger.LogInformation($"Released lock");
	}

	/// <summary>
	/// Performs a write -> read -> clear using the underlying persistence mechanism and
	/// throws an <see cref="MsalCachePersistenceException"/> if something goes wrong.
	/// </summary>
	/// <remarks>Does not overwrite the token cache. Should never fail on Windows and Mac where the cache accessors are guaranteed to exist by the OS.</remarks>
	public void VerifyPersistence()
	{
		CacheStore.VerifyPersistence();
	}
}



