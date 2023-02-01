namespace Uno.Extensions;

public static class HostBuilderContextExtensions
{
	public static bool IsRegistered(this HostBuilderContext context, string registeredKey, bool  newIsRegistered = true)
	{
		return context.Properties.IsRegistered(registeredKey, newIsRegistered);
	}
}
