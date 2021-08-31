using System;

namespace Uno.Extensions.Navigation
{
    public interface INavigationService
    {
        bool CanNavigate(NavigationRequest request);
        NavigationResult Navigate(NavigationRequest request);
    }

    public interface INavigationManager : INavigationService
        {
        INavigationService ActivateAdapter<TControl>(TControl control);
        void DeactivateAdapter(INavigationService adapter);
    }
}
