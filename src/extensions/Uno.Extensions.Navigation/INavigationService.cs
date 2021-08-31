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
        INavigationService AddAdapter<TControl>(TControl control, bool enabled);

        void ActivateAdapter(INavigationService adapter);

        void DeactivateAdapter(INavigationService adapter, bool cleanup = true);
    }
}
