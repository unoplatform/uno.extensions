using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationContext(
    IServiceProvider Services,
    NavigationRequest Request,
    CancellationTokenSource CancellationSource,
    RouteMap Mapping,
    bool CanCancel = true)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public INavigator Navigation => Services.GetService<INavigator>();

    public CancellationToken CancellationToken => CancellationSource.Token;

    public void Cancel()
    {
        if (CanCancel)
        {
            CancellationSource.Cancel();
        }
    }

    public bool IsCancelled => CancellationToken.IsCancellationRequested || (Request.Cancellation?.IsCancellationRequested ?? false);

    public object CreateViewModel()
    {
        var context = this;
        var services = context.Services;
        var mapping = context.Mapping;
        if (mapping?.ViewModel is not null)
        {
            var dataFactor = services.GetService<ViewModelDataProvider>();
            dataFactor.Parameters = context.Request.Route.Data;

            var vm = services.GetService(mapping.ViewModel);
            if (vm is IInjectable<INavigator> navAware)
            {
                navAware.Inject(context.Navigation);
            }

            if (vm is IInjectable<IServiceProvider> spAware)
            {
                spAware.Inject(context.Services);
            }

            return vm;
        }

        return null;
    }
}
