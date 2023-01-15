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
				var defaultCulture = ApplicationLanguages.PrimaryLanguageOverride ??
							CultureInfo.DefaultThreadCurrentUICulture?.Name ??
							CultureInfo.DefaultThreadCurrentCulture?.Name ??
							_uiThread?.CurrentUICulture?.Name ??
							_uiThread?.CurrentCulture?.Name;
				settingsCulture = string.IsNullOrWhiteSpace(defaultCulture) ?
									SupportedCultures.First() :
									SupportedCultures.FirstOrDefault(x => x.Name == defaultCulture) ??      // Handles full culture match  eg en-AU == en-AU
										SupportedCultures.FirstOrDefault(x => x.Name.StartsWith(defaultCulture)) ?? // Handles language only match eg en-AU.StartsWith(en)
										SupportedCultures.First();

			}
			return settingsCulture;
		}
	}

	/// <inheritdoc/>
	public async Task SetCurrentCultureAsync(CultureInfo newCulture)
	{
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
			if (ApplicationLanguages.PrimaryLanguageOverride != culture.Name)
			{
				ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
			}
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
