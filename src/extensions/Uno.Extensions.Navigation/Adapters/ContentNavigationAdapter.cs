using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public class ContentNavigationAdapter : BaseNavigationAdapter<ContentControl>
    {
        private IContentWrapper ContentHost => ControlWrapper as IContentWrapper;

        public ContentNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IContentWrapper contentWrapper) : base(services,navigationMapping,contentWrapper)
        {
        }

        public override NavigationResult Navigate(NavigationContext context)
        {
            var request = context.Request;
            var path = context.Path;
            Debug.WriteLine("Navigation: " + path);

            var map = Mapping.LookupByPath(path);

            var vm = map?.ViewModel is not null ? context.Services.GetService(map.ViewModel) : null;

            var view = ContentHost.ShowContent(map.View, vm);

            if (view is INavigationAware navAware)
            {
                navAware.Navigation = Navigation;
            }

            return new NavigationResult(request, Task.CompletedTask, null);
        }
    }
}
