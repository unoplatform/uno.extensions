using System;
using System.Collections.Generic;
using System.Linq;
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

        public NavigationMap LookupByViewModel(Type viewModelType)
        {
            return Mappings.First(x => x.Value.ViewModel == viewModelType).Value;
        }

        public NavigationMap LookupByView(Type viewType)
        {
            return Mappings.First(x => x.Value.View == viewType).Value;
        }

        public NavigationMap LookupByData(Type dataType)
        {
            return Mappings.First(x => x.Value.Data == dataType).Value;
        }
    }
}
