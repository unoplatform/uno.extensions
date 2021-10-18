using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Services;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation;

public static class RegionNavigationServiceExtensions
{
    //public static IControlNavigationServiceFactory FindForControl(this IDictionary<Type, IControlNavigationServiceFactory> factories, object control)
    //{
    //    var controlType = control.GetType();
    //    return factories.FindForControlType(controlType);
    //}

    //public static IControlNavigationServiceFactory FindForControlType(this IDictionary<Type, IControlNavigationServiceFactory> factories, Type controlType)
    //{
    //    if (factories.TryGetValue(controlType, out var factory))
    //    {
    //        return factory;
    //    }

    //    var baseTypes = controlType.GetBaseTypes().ToArray();
    //    for (var i = 0; i < baseTypes.Length; i++)
    //    {
    //        if (factories.TryGetValue(baseTypes[i], out var baseFactory))
    //        {
    //            return baseFactory;
    //        }
    //    }

    //    return null;
    //}

    public static void InjectServicesAndSetDataContext(this object view, IServiceProvider services, INavigationService navigation, object viewModel)
    {
        if (view is FrameworkElement fe)
        {
            //fe.SetServiceProvider(services);

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
