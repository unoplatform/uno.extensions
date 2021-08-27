#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
#endif

namespace Uno.Extensions.Navigation
{
    public interface INavigationMapping
    {
        void Register(NavigationMap map);
        NavigationMap LookupByPath(string path);
    }

}
