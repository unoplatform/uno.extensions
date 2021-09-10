using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters
{
    public class ContentNavigationAdapter : BaseNavigationAdapter
    {
        public ContentNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IContentWrapper contentWrapper) : base(services, navigationMapping, contentWrapper)
        {
        }
    }
}
