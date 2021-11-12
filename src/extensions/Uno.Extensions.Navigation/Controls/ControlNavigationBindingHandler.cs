using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !WINUI
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public abstract class ControlNavigationBindingHandler<TControl> : INavigationBindingHandler
    {
        public abstract void Bind(FrameworkElement view);

        public bool CanBind(FrameworkElement view)
        {
            var viewType = view.GetType();
            if (viewType == typeof(TControl))
            {
                return true;
            }

            var baseTypes = viewType.GetBaseTypes();
            return baseTypes.Any(baseType => baseType == typeof(TControl));
        }
    }


}
