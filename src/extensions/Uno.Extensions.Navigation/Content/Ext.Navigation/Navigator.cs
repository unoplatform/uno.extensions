using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public record Navigator(ILogger<Navigator> Logger, Func<Frame> NavigationFrameFunc) : INavigator
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        /// <summary>
        /// Gets current navigation frame.
        /// This used to cache the result but in scenario where there are multiple
        /// frames (eg tabs) this needs to return the active frame
        /// </summary>
        private Frame NavigationFrame => NavigationFrameFunc();

        public void Navigate(Type destinationPage, object viewModel = null)
        {
            Logger.LazyLogDebug(() => $"Dispatching");
#if !WINUI
            _ = NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
#else
            _ = NavigationFrame.DispatcherQueue.TryEnqueue(() =>
#endif
            {
                Logger.LazyLogDebug(() => $"Navigating to {destinationPage.Name}");
                var navResults = NavigationFrame.Navigate(destinationPage);
                if (navResults && viewModel is not null)
                {
                    Logger.LazyLogDebug(() => $"Setting DataContext of type {viewModel.GetType().Name}");
                    (NavigationFrame.Content as Page).DataContext = viewModel;
                }
            });
        }

        public void GoBack(object viewModelForPreviousPage = null)
        {
            Logger.LazyLogDebug(() => $"Dispatching");
#if !WINUI
            _ = NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
#else
            _ = NavigationFrame.DispatcherQueue.TryEnqueue(() =>
#endif
            {
                Logger.LazyLogDebug(() => $"Navigating back to previous page");
                NavigationFrame.GoBack();
                Logger.LazyLogDebug(() => $"New page is {NavigationFrame.Content?.GetType()?.Name}");
                var current = NavigationFrame.Content as Page;
                if (current is not null &&
                    viewModelForPreviousPage is not null &&
                    current.DataContext != viewModelForPreviousPage)
                {
                    Logger.LazyLogDebug(() => $"Restoring DataContext of type {viewModelForPreviousPage.GetType().Name}");
                    current.DataContext = viewModelForPreviousPage;
                }
            });
        }

        public void Clear()
        {
            Logger.LazyLogDebug(() => $"Dispatching");
#if !WINUI
            _ = NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
#else
            _ = NavigationFrame.DispatcherQueue.TryEnqueue(() =>
#endif
            {
                Logger.LazyLogDebug(() => $"Clearing backstack of length {NavigationFrame.BackStack.Count}");
                NavigationFrame.BackStack.Clear();
            });
        }
    }
}
