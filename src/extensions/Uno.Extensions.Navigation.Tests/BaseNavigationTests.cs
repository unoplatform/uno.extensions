using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Navigation.Tests;

public class BaseNavigationTests
{
    protected INavigationService Navigation { get; private set; }

    [TestInitialize]
    public void InitializeTests()
    {
        ServiceCollection services = new();
        services.AddNavigation();

        InitializeServices(services);

        var sp = services.BuildServiceProvider();
        Navigation = sp.GetService<INavigationService>();
        var mapping = sp.GetService<IRouteMappings>();
        mapping.Register(new RouteMap(typeof(PageOne).Name, typeof(PageOne)));
    }

    protected virtual void InitializeServices(IServiceCollection services)
    {
    }
}
