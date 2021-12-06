//using CommunityToolkit.Mvvm.Messaging;
//#if !WINDOWS_UWP && !WINUI
//using Popup = Windows.UI.Xaml.Controls.Popup;
//#endif
#if !WINUI
#else
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.Extensions.Navigation;

public class RegionControlProvider
{
	public object? RegionControl { get; set; }
}
