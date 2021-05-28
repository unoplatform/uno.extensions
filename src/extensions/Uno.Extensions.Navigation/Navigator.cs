using System;
#if WINDOWS_UWP
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation
{
    public record Navigator : INavigator
    {
        public Frame NavigationFrame { get; }

        public Navigator(Frame navFrame)
        {
            NavigationFrame = navFrame;
        }

        public void Navigate(Type destinationPage, object viewModel = null)
        {
#if !WINUI
            _ = NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
#else
            _ = NavigationFrame.DispatcherQueue.TryEnqueue(() =>
#endif
            {
                var navResults = NavigationFrame.Navigate(destinationPage);
                if (navResults && viewModel is not null)
                {
                    (NavigationFrame.Content as Page).DataContext = viewModel;
                }
            });
        }

        public void GoBack(object viewModelForPreviousPage = null)
        {
#if !WINUI
            _ = NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
#else
            _ = NavigationFrame.DispatcherQueue.TryEnqueue(() =>
#endif
            {
                NavigationFrame.GoBack();
                var current = NavigationFrame.Content as Page;
                if(current is not null && viewModelForPreviousPage is not null)
                {
                    current.DataContext = viewModelForPreviousPage;
                }
            });
        }

        public void Clear()
        {
#if !WINUI
            _ = NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
#else
            _ = NavigationFrame.DispatcherQueue.TryEnqueue(() =>
#endif
            {
                NavigationFrame.BackStack.Clear();
        });
        }
    }
}
