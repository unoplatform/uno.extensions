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
        private Frame Frame { get; set; }

        public void Inject(Frame control) => Frame = control; 

        public void GoBack()
        {
            Frame.GoBack();
        }

        public bool Navigate(Type sourcePageType, object parameter = null)
        {
            Debug.WriteLine("Backstack (Navigate - before): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));
            var nav = Frame.Navigate(sourcePageType, parameter);
            Debug.WriteLine("Backstack (Navigate - after): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));
            return nav;
        }

        public void RemoveLastFromBackStack()
        {
            Debug.WriteLine("Backstack (RemoveLastFromBackStack - before): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));
            Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
            Debug.WriteLine("Backstack (RemoveLastFromBackStack - after): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));
        }

        public void ClearBackStack()
        {
            Debug.WriteLine("Backstack (ClearBackStack - before): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));
            Frame.BackStack.Clear();
            Debug.WriteLine("Backstack (ClearBackStack - after): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));
        }
    }
}
