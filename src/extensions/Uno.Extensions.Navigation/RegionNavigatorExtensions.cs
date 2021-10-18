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

public static class RegionNavigatorExtensions
{
    public static void InjectServicesAndSetDataContext(this object view, IServiceProvider services, INavigator navigation, object viewModel)
    {
        if (view is FrameworkElement fe)
        {
            if (viewModel is not null &&
                fe.DataContext != viewModel)
            {
                fe.DataContext = viewModel;
            }
        }

        if (view is IInjectable<INavigator> navAware)
        {
            navAware.Inject(navigation);
        }

        if (view is IInjectable<IServiceProvider> spAware)
        {
            spAware.Inject(services);
        }
    }
}
