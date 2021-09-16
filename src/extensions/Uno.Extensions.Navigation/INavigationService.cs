using System;

namespace Uno.Extensions.Navigation;

public interface INavigationService
{
    NavigationResponse Navigate(NavigationRequest request);
}
