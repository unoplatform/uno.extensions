using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Controls
{
    public abstract class ControlRequestHandlerBase<TControl> : IRequestHandler
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
