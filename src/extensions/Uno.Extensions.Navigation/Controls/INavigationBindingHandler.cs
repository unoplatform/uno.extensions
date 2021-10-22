#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public interface INavigationBindingHandler
    {
        bool CanBind(FrameworkElement view);

        void Bind(FrameworkElement view);
    }
}
