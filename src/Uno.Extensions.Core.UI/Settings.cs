using System.Text.Json;

namespace Uno.Extensions;

internal record Settings(ILogger<Settings>? Logger = default) : ISettings
{
#if __WINDOWS__
	private const string SettingsFileName = "__settings__.";
#endif
	private bool _initialized;
	private bool _useFileSettings;
	private Dictionary<string, string> settings = new Dictionary<string, string>();

#if __WINDOWS__
	private string SettingsFile
	{
		get
		{
			var dataFolder = ApplicationDataExtensions.DataFolder();
			var settingsFile = Path.Combine(dataFolder, SettingsFileName);
			return settingsFile;
		}
	}
#endif

	private void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;

#if __WINDOWS__
		if (!PlatformHelper.IsAppPackaged)
		{
			_useFileSettings = true;
			var settingsFile = SettingsFile;
			if (File.Exists(settingsFile))
			{
				var json = File.ReadAllText(settingsFile);
				settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
			}
		}
#else
		_useFileSettings = false;
#endif
	}

	public string? Get(string key)
	{
		Initialize();
		if (!_useFileSettings)
		{
			return ApplicationData.Current.LocalSettings.Values[key] is string t ? t : default;
		}
		else
		{
			return settings.TryGetValue(key, out var value) ? value : default;
		}
	}
	public void Set(string key, string? value)
	{
		Initialize();
		if (!_useFileSettings)
		{
			ApplicationData.Current.LocalSettings.Values[key] = value;
		}
		else
		{
			if (value is null)
			{
				settings.Remove(key);
			}
			else
			{
				settings[key] = value;
			}
#if __WINDOWS__
			File.WriteAllText(SettingsFile, JsonSerializer.Serialize(settings));
#endif
		}
	}

	public void Remove(string key) => Set(key, null);

	public void Clear()
	{
		Initialize();
		if (!_useFileSettings)
		{
			ApplicationData.Current.LocalSettings.Values.Clear();
		}
		else
		{
			settings.Clear();
#if __WINDOWS__
			File.WriteAllText(SettingsFile, JsonSerializer.Serialize(settings));
#endif
		}
	}

	public IReadOnlyCollection<string> Keys
	{
		get
		{
			Initialize();
			return _useFileSettings ? settings.Keys : ApplicationData.Current.LocalSettings.Values.Keys.Select(k => k.ToString()).ToArray();
		}
	}
}
