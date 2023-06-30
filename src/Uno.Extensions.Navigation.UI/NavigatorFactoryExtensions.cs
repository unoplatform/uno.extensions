namespace Uno.Extensions.Navigation;

internal static class NavigatorFactoryExtensions
{
    public static Type? FindRequestServiceByType(this NavigatorFactory factory, Type viewType)
    {
        if (factory.Navigators.TryGetValue(viewType.Name, out var serviceType) && serviceType.Item2)
        {
            return serviceType.Item1;
        }
        var baseTypes = viewType.GetBaseTypes();
        foreach (var type in baseTypes)
        {
            if (factory.Navigators.TryGetValue(type.Name, out var baseServiceType) && baseServiceType.Item2)
            {
                return baseServiceType.Item1;
            }

        }

        return null;
    }
}
