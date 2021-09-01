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

        public void Inject(ContentControl control)
        {
            ContentHost.Inject(control);
        }

        public ContentNavigationAdapter(
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IContentWrapper contentWrapper)
        {
            Services = services.CreateScope().ServiceProvider;
            Mapping = navigationMapping;
            ContentHost = contentWrapper;
        }

        public bool CanNavigate(NavigationRequest request)
        {
            return true;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            var map = Mapping.LookupByPath(path);

            var vm =  map.ViewModel is not null ? Services.GetService(map.ViewModel) : null;

            ContentHost.ShowContent(map.View, vm);

            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
