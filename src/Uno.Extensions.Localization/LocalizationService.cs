using Uno.Extensions.Hosting;

namespace Uno.Extensions.Localization;

public class LocalizationService : IServiceInitialize
{
	private static string DefaultCulture = "en-US";

	private Thread? _uiThread;

	private readonly ILogger _logger;

	private readonly IOptionsMonitor<LocalizationSettings> _settings;

	private IDisposable? _settingsListener;

	private CultureInfo[] SupportedCultures { get; }

	private CultureInfo CurrentCulture =>
		_settings?.CurrentValue?.CurrentCulture?.AsCulture() ??
		SupportedCultures.First();

	public LocalizationService(
		ILogger<LocalizationService> logger,
		IOptions<LocalizationConfiguration> configuration,
		IOptionsMonitor<LocalizationSettings> settings)
	{
		_logger = logger;
		_settings = settings;
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
}
