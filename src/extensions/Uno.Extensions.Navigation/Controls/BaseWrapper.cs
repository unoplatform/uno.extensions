#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
#else
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public abstract class BaseWrapper<TControl> : IInjectable<TControl>
    {
        protected TControl Control { get; private set; }

        public void Inject(TControl control) => Control = control;

        public abstract NavigationContext CurrentContext { get; }
    }
}
