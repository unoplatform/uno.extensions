#if WINUI
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Markup;
global using Microsoft.UI.Xaml.Data;
global using Microsoft.UI.Xaml.Media;
global using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
global using DispatcherQueueHandler = Microsoft.UI.Dispatching.DispatcherQueueHandler;
global using CurrentChangingEventHandler = Microsoft.UI.Xaml.Data.CurrentChangingEventHandler;
global using CurrentChangingEventArgs = Microsoft.UI.Xaml.Data.CurrentChangingEventArgs;
global using CurrentChangedEventHandler = System.EventHandler<object?>;
#else
global using Windows.UI.Xaml;
global using Windows.UI.Xaml.Controls;
global using Windows.UI.Xaml.Markup;
global using Windows.UI.Xaml.Data;
global using Windows.UI.Xaml.Media;
global using DispatcherQueue = Windows.System.DispatcherQueue;
global using DispatcherQueueHandler = Windows.System.DispatcherQueueHandler;
global using CurrentChangingEventHandler = Windows.UI.Xaml.Data.CurrentChangingEventHandler;
global using CurrentChangingEventArgs = Windows.UI.Xaml.Data.CurrentChangingEventArgs;
global using CurrentChangedEventHandler = System.EventHandler<object?>;
#endif

#if WINUI && (WINDOWS || (NET6_0_OR_GREATER && (__IOS__ || __ANDROID__)))
global using WinRT;
#else
global using System.Runtime.InteropServices.WindowsRuntime;
#endif
