using Uno.Extensions.Navigation.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
#else
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public abstract class BaseNavigationAdapter<TControl>: INavigationAdapter<TControl>
    {
        protected IInjectable<TControl> ControlWrapper { get; }

        public string Name { get; set; }

        protected INavigationMapping Mapping { get; }

        protected IServiceProvider Services { get; }

        public INavigationService Navigation { get; set; }

        public abstract NavigationResult Navigate(NavigationContext context);

        public void Inject(TControl control)
        {
            ControlWrapper.Inject(control);
        }

        public BaseNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IInjectable<TControl> control)
        {
            Services = services.CreateScope().ServiceProvider;
            Mapping = navigationMapping;
            ControlWrapper = control;
        }
    }
}
