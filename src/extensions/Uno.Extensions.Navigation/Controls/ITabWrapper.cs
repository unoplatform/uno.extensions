using System;
#if WINDOWS_UWP 
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public interface ITabWrapper : IInjectable<TabView>
{
    bool ContainsTab(string tabName);

    object ActivateTab(string tabName, object viewModel);
}
