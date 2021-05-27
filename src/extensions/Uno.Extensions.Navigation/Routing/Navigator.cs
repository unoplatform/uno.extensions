using System;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
using Windows.UI.Xaml.Controls;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace Uno.Extensions.Navigation
{
    public record Navigator : INavigator
    {
        public Frame NavigationFrame { get; }

        public Navigator(Frame navFrame)
        {
            NavigationFrame = navFrame;
        }

        public void Navigate(Type destinationPage, object? viewModel=null)
        {
//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
            NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
            //-:cnd:noEmit
#else
//+:cnd:noEmit
                NavigationFrame.DispatcherQueue.TryEnqueue(
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
            () => {
                var navResults = NavigationFrame.Navigate(destinationPage);
                if (navResults && viewModel is not null)
                {
                    (NavigationFrame.Content as Page).DataContext = viewModel;
                }
            });
        }

        public void GoBack()
        {
            //-:cnd:noEmit
#if WINDOWS_UWP
            //+:cnd:noEmit
            NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
            //-:cnd:noEmit
#else
//+:cnd:noEmit
                NavigationFrame.DispatcherQueue.TryEnqueue(
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
            () => {
                NavigationFrame.GoBack();
            });
        }

        public void Clear()
        {
            //-:cnd:noEmit
#if WINDOWS_UWP
            //+:cnd:noEmit
            NavigationFrame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
            //-:cnd:noEmit
#else
            //+:cnd:noEmit
            NavigationFrame.DispatcherQueue.TryEnqueue(
        //-:cnd:noEmit
#endif
        //+:cnd:noEmit
        () => {
            NavigationFrame.BackStack.Clear();
        });
        }
    }
}

