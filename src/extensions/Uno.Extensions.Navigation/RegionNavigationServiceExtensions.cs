using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation;

public static class RegionNavigationServiceExtensions
{

    public static IRegionFactory FindForControl(this IDictionary<Type, IRegionFactory> factories, object control)
    {
        var controlType = control.GetType();
        return factories.FindForControlType(controlType);
    }

    public static IRegionFactory FindForControlType(this IDictionary<Type, IRegionFactory> factories, Type controlType)
    {
        if (factories.TryGetValue(controlType, out var factory))
        {
            return factory;
        }

        var baseTypes = controlType.GetBaseTypes().ToArray();
        for (var i = 0; i < baseTypes.Length; i++)
        {
            if (factories.TryGetValue(baseTypes[i], out var baseFactory))
            {
                return baseFactory;
            }
        }

        return null;
    }

    public static void InjectServicesAndSetDataContext(this object view, IServiceProvider services, INavigationService navigation, object viewModel)
    {
        if (view is FrameworkElement fe)
        {
            fe.SetServiceProvider(services);

            if (viewModel is not null &&
                fe.DataContext != viewModel)
            {
                fe.DataContext = viewModel;
            }
        }

        if (view is IInjectable<INavigationService> navAware)
        {
            navAware.Inject(navigation);
        }

        if (view is IInjectable<IServiceProvider> spAware)
        {
            spAware.Inject(services);
        }
    }
}
