namespace Uno.Extensions.Validation.Tests;

/// <summary>
/// In-memory <see cref="IStringLocalizer"/> following the contract of the ResourceLoaderStringLocalizer: an unknown key returns the key itself, flagged as not found.
/// </summary>
internal sealed class FakeStringLocalizer(Dictionary<string, string> resources) : IStringLocalizer
{
	public Dictionary<string, string> Resources { get; } = resources;

	public LocalizedString this[string name]
		=> Resources.TryGetValue(name, out var value)
			? new LocalizedString(name, value)
			: new LocalizedString(name, name, resourceNotFound: true);

	public LocalizedString this[string name, params object[] arguments]
		=> this[name] is { ResourceNotFound: false } format
			? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, format.Value, arguments))
			: this[name];

	public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
		=> Resources.Select(kvp => new LocalizedString(kvp.Key, kvp.Value));
}
