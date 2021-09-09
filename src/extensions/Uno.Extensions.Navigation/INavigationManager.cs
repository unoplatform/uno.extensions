namespace Uno.Extensions.Navigation
{
    public interface INavigationManager : INavigationService
    {
        INavigationService AddAdapter<TControl>(INavigationService parentAdapter, string routeName, TControl control, INavigationService existingAdapter);

        void RemoveAdapter(INavigationService adapter);

        //INavigationService ScopedServiceForControl(object control);
    }
}
