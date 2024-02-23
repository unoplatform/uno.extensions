namespace Uno.Extensions;

internal interface ISettings
{
	string? Get(string key);
	void Set(string key, string value);
}
