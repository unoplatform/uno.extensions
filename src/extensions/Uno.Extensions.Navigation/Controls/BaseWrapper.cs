using System;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public abstract class BaseWrapper : IInjectable
    {
        protected object Control { get; private set; }

        public void Inject(object  control) => Control = control;

        protected static void InitialiseView(object view, NavigationContext context, object viewModel)
        {
            if (view is FrameworkElement fe)
            {
                //fe.SetContext(context);
                if (viewModel is not null && fe.DataContext != viewModel)
                {
                    fe.DataContext = viewModel;
                }
            }

            if (view is INavigationAware navAware)
            {
                navAware.Navigation = context.Services.GetService<INavigationService>();
            }
        }
    }
}
