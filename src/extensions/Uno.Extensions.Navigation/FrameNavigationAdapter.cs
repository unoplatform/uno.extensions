using System.Threading.Tasks;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation
{
    public class FrameNavigationAdapter : INavigationAdapter
    {
        public Frame NavigationFrame { get; set; }

        private INavigationMapping Mapping { get; }

        public FrameNavigationAdapter(INavigationMapping navigationMapping)
        {
            Mapping = navigationMapping;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            var navigationType = Mapping.LookupByPath(request.Route.Path.OriginalString);

            NavigationFrame.Navigate(navigationType.View);

            return new NavigationResult(request, Task.CompletedTask);
            }
    }

}
