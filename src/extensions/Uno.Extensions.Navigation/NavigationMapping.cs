using System.Collections.Generic;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
#endif

namespace Uno.Extensions.Navigation
{
    public class NavigationMapping : INavigationMapping
    {
        private IDictionary<string, NavigationMap> Mappings { get; } = new Dictionary<string, NavigationMap>();
        public void Register(NavigationMap map)
        {
            Mappings[map.Path] = map;
        }
        public NavigationMap LookupByPath(string path)
        {
            return Mappings[path];
        }

    }

}
