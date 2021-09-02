namespace Uno.Extensions.Navigation
{
    public interface INavigationManager : INavigationService
    {
        INavigationAdapter AddAdapter<TControl>(TControl control, bool enabled);

        void ActivateAdapter(INavigationAdapter adapter);

        void DeactivateAdapter(INavigationAdapter adapter, bool cleanup = true);
    }
}
