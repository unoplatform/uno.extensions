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
    bool ContainsTab(string tabName);

    object ActivateTab(string tabName, object viewModel);
}
