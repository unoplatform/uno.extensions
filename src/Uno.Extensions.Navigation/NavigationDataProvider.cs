using System.Collections.Generic;
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

public class NavigationDataProvider
{
	public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

	public TData? GetData<TData>()
		where TData : class
	{
		return (Parameters?.TryGetValue(string.Empty, out var data) ?? false) ? data as TData : default;
	}
}
