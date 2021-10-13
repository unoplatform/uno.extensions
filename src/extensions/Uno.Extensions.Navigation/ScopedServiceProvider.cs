using System;

namespace Uno.Extensions.Navigation;

public class ScopedServiceProvider : IScopedServiceProvider
{
    public IServiceProvider Services { get; }

    public ScopedServiceProvider(IServiceProvider services)
    {
        Services = services;
    }

    public object GetService(Type serviceType)
    {
        return Services.GetService(serviceType);
    }
}
