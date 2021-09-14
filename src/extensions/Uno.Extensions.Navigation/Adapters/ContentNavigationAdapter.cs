using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters;

public class ContentNavigationAdapter : NavigationAdapter<IContentWrapper>
{
    public ContentNavigationAdapter(
        // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
        IServiceProvider services,
        IContentWrapper contentWrapper) : base(services, contentWrapper)
    {
    }
}
