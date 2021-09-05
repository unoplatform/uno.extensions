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
    public class ContentNavigationAdapter : INavigationAdapter<ContentControl>
    {
        private INavigationMapping Mapping { get; }

        private IServiceProvider Services { get; }

        private IContentWrapper ContentHost { get; }

        public INavigationService Navigation { get; set; }

        public void Inject(ContentControl control)
        {
            ContentHost.Inject(control);
        }

        public ContentNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IContentWrapper contentWrapper)
        {
            Services = services.CreateScope().ServiceProvider;
            Mapping = navigationMapping;
            ContentHost = contentWrapper;
        }

        public bool CanNavigate(NavigationContext context)
        {
            var request = context.Request;
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            var map = Mapping.LookupByPath(path);
            if (map?.View is not null)
            {
                return !map.View.IsSubclassOf(typeof(Page));
            }
            return false;
        }

        public NavigationResult Navigate(NavigationContext context)
        {
            var request = context.Request;
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            var map = Mapping.LookupByPath(path);

            var vm = map.ViewModel is not null ? Services.GetService(map.ViewModel) : null;

            var view = ContentHost.ShowContent(map.View, vm);

            if (view is INavigationAware navAware)
            {
                navAware.Navigation = Navigation;
            }

            return new NavigationResult(request, Task.CompletedTask, null);
        }
    }
}
