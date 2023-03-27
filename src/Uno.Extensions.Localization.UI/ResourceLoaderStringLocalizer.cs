

#if WINDOWS
using Microsoft.Windows.ApplicationModel.Resources;
#endif
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Localization;

/// <summary>
/// This implementation of <see cref="IStringLocalizer"/> uses ResourceLoader on Uno and ResourceManager on WinAppSdk
/// to get the string resources.
/// </summary>
public class ResourceLoaderStringLocalizer : IStringLocalizer
{
	private const string SearchLocation = "Resources";
#if WINDOWS
	private readonly ResourceMap _defaultResourceMap;
	private readonly ResourceMap _appResourceMap;
#else
	private readonly ResourceLoader _defaultResourceLoader;
	private readonly ResourceLoader? _appResourceLoader;

#endif
	private readonly bool _treatEmptyAsNotFound;

	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceLoaderStringLocalizer"/> class.
	/// </summary>
	/// <param name="appHostEnvironment">Application host environment - used to retrieve assembly where resources defined</param>
	/// <param name="treatEmptyAsNotFound">If empty strings should be treated as not found.</param>
	public ResourceLoaderStringLocalizer(IAppHostEnvironment appHostEnvironment, bool treatEmptyAsNotFound = true)
	{
		_treatEmptyAsNotFound = treatEmptyAsNotFound;
#if WINDOWS
		var mainResourceMap = new ResourceManager().MainResourceMap;
		// TryGetSubtree can return null if no resources found, so defalut to main resource map if not found
		_defaultResourceMap = mainResourceMap.TryGetSubtree("Resources") ?? mainResourceMap;
		_appResourceMap = mainResourceMap.TryGetSubtree(appHostEnvironment.HostAssembly?.GetName().Name).TryGetSubtree("Resources") ?? mainResourceMap;
#else
		_defaultResourceLoader = ResourceLoader.GetForViewIndependentUse();
		try
		{
			_appResourceLoader = new ResourceLoader($"{appHostEnvironment.HostAssembly?.GetName().Name}/Resources");
		}
		catch { }
#endif
	}

	/// <inheritdoc/>
	public LocalizedString this[string name] => GetLocalizedString(name);

	/// <inheritdoc/>
	public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

	/// <inheritdoc/>
	public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
		=> throw new NotSupportedException("ResourceLoader doesn't support listing all strings.");

	private LocalizedString GetLocalizedString(string name, params object[] arguments)
	{
		if (name is null)
		{
			throw new ArgumentNullException(nameof(name));
		}

#if WINDOWS
		var resource = _appResourceMap.GetValue(name)?.ValueAsString ??
						_defaultResourceMap.GetValue(name)?.ValueAsString;
#else
		var resource = _appResourceLoader?.GetString(name) ??
			_defaultResourceLoader.GetString(name);
#endif

		if (_treatEmptyAsNotFound &&
			string.IsNullOrEmpty(resource))
		{
			resource = null;
		}

		var notFound = resource == null;

		resource ??= name;

		var value = arguments.Any()
			? string.Format(CultureInfo.CurrentCulture, resource, arguments)
			: resource;

		return new LocalizedString(name, value, resourceNotFound: notFound, searchedLocation: SearchLocation);
	}
}
