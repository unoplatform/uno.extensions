using System;

namespace Uno.Extensions.Navigation;

public interface INavigationService
{
    NavigationResponse NavigateAsync(NavigationRequest request);
}
