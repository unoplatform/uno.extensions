using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
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
            IContentWrapper contentWrapper) : base(services, navigationMapping, contentWrapper)
        {
        }
    }
}
