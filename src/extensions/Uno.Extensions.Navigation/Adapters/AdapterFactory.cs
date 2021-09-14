using System;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation.Adapters;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record AdapterFactory<TControl, TAdapter> : IAdapterFactory
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    where TAdapter : INavigationAdapter
{
    public Type ControlType => typeof(TControl);

    public INavigationAdapter Create(IServiceProvider services)
    {
        return services.GetService<TAdapter>();
    }
}
