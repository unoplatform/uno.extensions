using System;

namespace Uno.Extensions.Navigation
{
    public interface INavigationService
    {
        NavigationResponse Navigate(NavigationRequest request);

        INavigationService Parent { get; }

        INavigationService Nested(string routeName = null);
    }
}
