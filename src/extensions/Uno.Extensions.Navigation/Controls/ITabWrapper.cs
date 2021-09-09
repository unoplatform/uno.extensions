using System;
#if WINDOWS_UWP
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public interface ITabWrapper : IInjectable<TabView>
{
    string CurrentTabName { get; }

    bool ContainsTab(string tabName);

    object ActivateTab(NavigationContext context, string tabName, object viewModel);
}
