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
        public const string PreviousViewUri = "..";

        public Frame NavigationFrame { get; set; }

        private INavigationMapping Mapping { get; }

        public FrameNavigationAdapter(INavigationMapping navigationMapping)
        {
            Mapping = navigationMapping;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            if (path == PreviousViewUri)
            {
                NavigationFrame.GoBack();
            }
            else
            {
                var navigationType = Mapping.LookupByPath(path);

                NavigationFrame.Navigate(navigationType.View);
            }
            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
