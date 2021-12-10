#if WINUI
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Markup;
global using Microsoft.UI.Xaml.Data;
global using Microsoft.UI.Xaml.Media;
global using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
global using DispatcherQueueHandler = Microsoft.UI.Dispatching.DispatcherQueueHandler;
#else
global using Windows.UI.Xaml;
global using Windows.UI.Xaml.Controls;
global using Windows.UI.Xaml.Markup;
global using Windows.UI.Xaml.Data;
global using Windows.UI.Xaml.Media;
global using DispatcherQueue = Windows.System.DispatcherQueue;
global using DispatcherQueueHandler = Windows.System.DispatcherQueueHandler;
#endif
