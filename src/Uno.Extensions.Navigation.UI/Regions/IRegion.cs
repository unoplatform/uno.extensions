using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
#if !WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions
{
    public interface IRegion
    {
        string? Name { get; }

        FrameworkElement? View { get; }

        IServiceProvider? Services { get; }

        IRegion? Parent { get; }

        ICollection<IRegion> Children { get; }

        void ReassignParent();
    }
}
