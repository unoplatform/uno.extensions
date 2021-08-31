using System;
using System.Diagnostics;
using System.Linq;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public class FrameWrapper : IFrameWrapper
    {
        public Frame NavigationFrame { get; set; }

        public void GoBack()
        {
            NavigationFrame.GoBack();
        }

        public bool Navigate(Type sourcePageType, object parameter = null)
        {
            Debug.WriteLine("Backstack (Navigate - before): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            var nav = NavigationFrame.Navigate(sourcePageType, parameter);
            Debug.WriteLine("Backstack (Navigate - after): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            return nav;
        }

        public void RemoveLastFromBackStack()
        {
            Debug.WriteLine("Backstack (RemoveLastFromBackStack - before): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            NavigationFrame.BackStack.RemoveAt(NavigationFrame.BackStack.Count - 1);
            Debug.WriteLine("Backstack (RemoveLastFromBackStack - after): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
        }

        public void ClearBackStack()
        {
            Debug.WriteLine("Backstack (ClearBackStack - before): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            NavigationFrame.BackStack.Clear();
            Debug.WriteLine("Backstack (ClearBackStack - after): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
        }
    }
}
