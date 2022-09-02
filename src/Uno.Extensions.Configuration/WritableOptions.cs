namespace Uno.Extensions.Configuration;

public class WritableOptions<T> : IWritableOptions<T>
	where T : class, new()
{
	private const int MaxWriteRetries = 100;

	private readonly IOptionsMonitor<T> _options;

	private readonly string _section;

	private readonly string _file;

	private readonly Reloader _reloader;

	private readonly ILogger _logger;

	public WritableOptions(
		ILogger<IWritableOptions<T>> logger,
		Reloader reloader,
		IOptionsMonitor<T> options,
		string section,
		string file)
	{
		_logger = logger;
		_reloader = reloader;
		_options = options;
		_section = section;
		_file = file;
	}

	public T Value
	{
		get
		{
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage("Get current value");
			return _options.CurrentValue;
		}
	}

	public T Get(string name)
	{
		if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($@"Get options with name '{name}'");
		return _options.Get(name);
	}

	public async Task UpdateAsync(Func<T, T> applyChanges)
	{
		if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($@"Updating options, saving to file '{_file}'");

		var physicalPath = _file;
		var jObject = File.Exists(physicalPath) ? JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(physicalPath)) : new Dictionary<string, object>();
		jObject = jObject ?? new Dictionary<string, object>();
		var sectionObject = Value ?? new T();

		sectionObject = applyChanges?.Invoke(sectionObject) ?? new T();

		jObject[_section] = sectionObject;

		var json = JsonSerializer.Serialize(jObject);
		var dir = Path.GetDirectoryName(physicalPath);
		if (dir is not null && !Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		var written = false;
		var attempt = 0;
		while (!written && attempt++ < MaxWriteRetries)
		{
			await Reloader.ReadWriteLock.WaitAsync();
			try
			{
				written = true;
				File.WriteAllText(physicalPath, json);
			}
			catch(IOException)
			{
				// Only retry on IOExceptions eg sharing violation
				written = false;
			}
			finally
			{
				Reloader.ReadWriteLock.Release();
			}
			if(!written)
			{
				await Task.Yield();
			}
		}


		await _reloader.ReloadAllFileConfigurationProviders(physicalPath);
	}
}
