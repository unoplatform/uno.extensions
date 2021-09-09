#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public interface IInjectable<TControl>
    {
        void Inject(TControl control);

        NavigationContext CurrentContext { get; }
    }
}
