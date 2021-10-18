using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
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
        string Name { get; }

        FrameworkElement View { get; }

        IServiceProvider Services { get; }

        IRegion Parent { get; set; }

        void Attach(IRegion childRegion);

        void Detach(IRegion childRegion);

        Task<IEnumerable<(IRegion, NavigationRequest)>> GetChildren(Func<IRegion, (IRegion, NavigationRequest)> predicate, bool isBlocking);

        // TODO: Work out how we can remove these
        void AttachAll(IEnumerable<IRegion> children);
        IEnumerable<IRegion> DetachAll();
    }
}
