namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static bool IsRegistered(this IHostBuilder builder, string registeredKey, bool newIsRegistered = true)
	{
		return builder.Properties.IsRegistered(registeredKey, newIsRegistered);
	}

	internal static bool IsRegistered(this IDictionary<object,object> properties, string registeredKey, bool newIsRegistered = true)
	{
		if (properties.TryGetValue(registeredKey, out var value) &&
			value is bool registeredValue &&
			registeredValue)
		{
			return true;
		}
		properties[registeredKey] = newIsRegistered;
		return false;
	}

}
