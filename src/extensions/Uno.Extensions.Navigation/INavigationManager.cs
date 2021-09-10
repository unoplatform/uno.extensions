namespace Uno.Extensions.Navigation
{
    public interface INavigationManager : INavigationService
    {
        INavigationService AddAdapter(INavigationService parentAdapter, string routeName, object control, INavigationService existingAdapter);

        void RemoveAdapter(INavigationService adapter);

        //INavigationService ScopedServiceForControl(object control);
    }
}
