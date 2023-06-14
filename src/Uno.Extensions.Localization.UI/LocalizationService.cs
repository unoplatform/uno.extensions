using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Localization;

public class LocalizationService : IServiceInitialize, ILocalizationService, IDisposable
{
	private static string DefaultCulture = "en-US";

	private Thread? _uiThread;

	private readonly ILogger _logger;

	private readonly IOptionsMonitor<LocalizationSettings> _settings;
	private readonly IWritableOptions<LocalizationSettings> _writeSettings;

	private IDisposable? _settingsListener;

	/// <inheritdoc/>
	public CultureInfo[] SupportedCultures { get; }

	/// <inheritdoc/>
	public CultureInfo CurrentCulture
	{
		get
		{
			var settingsCulture = _settings?.CurrentValue?.CurrentCulture?.AsCulture();
			if (settingsCulture is null)
			{
				var defaultLanguage = PrimaryLanguageOverride ?? string.Empty;

				var defaultCulture =
							CultureInfo.DefaultThreadCurrentUICulture ??
							CultureInfo.DefaultThreadCurrentCulture ??
							_uiThread?.CurrentUICulture ??
							_uiThread?.CurrentCulture;

				if (!string.IsNullOrWhiteSpace(defaultLanguage))
				{
					settingsCulture =
										SupportedCultures.FirstOrDefault(x => x.Name == defaultLanguage) ??      // Handles full culture match  eg en-AU == en-AU
											SupportedCultures.FirstOrDefault(x => x.Name.StartsWith(defaultLanguage)); // Handles language only match eg en-AU.StartsWith(en)
				}


				settingsCulture ??=
									defaultCulture is null ?
									SupportedCultures.First() :
									SupportedCultures.FirstOrDefault(x => x.Name == defaultCulture.Name) ??      // Handles full culture match  eg en-AU == en-AU
										SupportedCultures.FirstOrDefault(x => x.Name.StartsWith(defaultCulture.Name)) ?? // Handles language only match eg en-AU.StartsWith(en)
										SupportedCultures.FirstOrDefault(x => x.Name.StartsWith(defaultCulture.TwoLetterISOLanguageName)) ?? // Handles language only match eg en-AU.StartsWith(en) (where defaultCulture is en-AU)
										SupportedCultures.First();
			}
			return settingsCulture;
		}
	}

	private static bool overrideSupported = true;
	private string? PrimaryLanguageOverride
	{
		get
		{
			if (!overrideSupported)
			{
				return default;
			}
			try
			{
				return ApplicationLanguages.PrimaryLanguageOverride;
			}
			catch (InvalidOperationException)
			{
				// This exception is raised on WinUI when unpackaged
				overrideSupported = false;
				return default;
			}
		}
		set
		{
			if (!overrideSupported)
			{
				return;
			}
			try
			{
				ApplicationLanguages.PrimaryLanguageOverride = value;
			}
			catch (InvalidOperationException)
			{
				// This exception is raised on WinUI when unpackaged
				overrideSupported = false;
			}
		}
	}

	/// <inheritdoc/>
	public async Task SetCurrentCultureAsync(CultureInfo newCulture)
	{
		// Change the application language for resource loading
		PrimaryLanguageOverride= newCulture.Name;

		await _writeSettings.UpdateAsync(langSetting => langSetting with { CurrentCulture = newCulture.Name });
	}

	public LocalizationService(
		ILogger<LocalizationService> logger,
		IOptions<LocalizationConfiguration> configuration,
		IOptionsMonitor<LocalizationSettings> settings,
		IWritableOptions<LocalizationSettings> writeSettings)
	{
		_logger = logger;
		_settings = settings;
		_writeSettings = writeSettings;

		SupportedCultures = configuration.Value?.Cultures?.AsCultures() ?? new[] { DefaultCulture.AsCulture()! };
	}

	public void Initialize()
	{
		_uiThread = Thread.CurrentThread;
		ApplyCurrentCulture();
		_settingsListener = OptionsMonitorExtensions.OnChange(_settings, delegate
		{
			ApplyCurrentCulture(false);
		});
	}

	private void ApplyCurrentCulture(bool updateThreadCulture = true)
	{
		try
		{
			var culture =
				((CurrentCulture is not null) ?
				PickSupportedCulture(CurrentCulture) :
				PickSupportedCulture(CultureInfo.CurrentCulture)) ?? new CultureInfo(DefaultCulture);
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
			if (updateThreadCulture &&
				_uiThread is not null)
			{
				_uiThread.CurrentCulture = culture;
				_uiThread.CurrentUICulture = culture;
			}
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to apply the culture override.");
		}
	}

	private CultureInfo? PickSupportedCulture(CultureInfo culture)
	{
		return
			SupportedCultures.FirstOrDefault(supported => supported.Name == culture.Name) ??
			SupportedCultures.FirstOrDefault(supported => supported.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName);
	}

	public void Dispose()
	{
		_settingsListener?.Dispose();
		_settingsListener = null;
	}
}
