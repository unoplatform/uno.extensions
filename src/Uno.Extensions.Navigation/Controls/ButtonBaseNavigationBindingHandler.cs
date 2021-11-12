#if !WINUI
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public class ButtonBaseNavigationBindingHandler : ActionNavigationBindingHandlerBase<ButtonBase>
    {
        public override void Bind(FrameworkElement view)
        {
            var viewButton = view as ButtonBase;
            if (viewButton is null)
            {
                return;
            }

            BindAction(viewButton,
                action => new RoutedEventHandler((sender, args) => action((ButtonBase)sender)),
                (element, handler) => element.Click += handler,
                (element, handler) => element.Click -= handler);
        }
    }
}
