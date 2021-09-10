using System;
using System.Diagnostics;
using System.Linq;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
#endif

namespace Uno.Extensions.Navigation.Controls
{

    public class FrameWrapper : BaseWrapper, IFrameWrapper
    {
        private Frame Frame => Control as Frame;

        private void GoBack(NavigationContext context, object parameter, object viewModel)
        {
            if (parameter is not null)
            {
                var entry = Frame.BackStack.Last();
                var newEntry = new PageStackEntry(entry.SourcePageType, parameter, entry.NavigationTransitionInfo);
                Frame.BackStack.Remove(entry);
                Frame.BackStack.Add(newEntry);
            }

            Frame.GoBack();

            InitialiseView(Frame.Content, context, viewModel);

            //var current = Frame.Content as FrameworkElement;
            //if (current is not null &&
            //    viewModel is not null &&
            //    current.DataContext != viewModel)
            //{
            //    current.DataContext = viewModel;
            //}

            //return Frame.Content;
        }

        public void Navigate(NavigationContext context, bool isBackNavigation, object viewModel)
        //public object Navigate(NavigationContext context, Type sourcePageType, object parameter, object viewModel)
        {
            if (isBackNavigation)
            {
                GoBack(context, context.Data, viewModel);
                return;
            }
            Debug.WriteLine("Backstack (Navigate - before): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));
            var nav = Frame.Navigate(context.Mapping.View, context.Data);
            Debug.WriteLine("Backstack (Navigate - after): " + string.Join(",", Frame.BackStack.Select(x => x.SourcePageType.Name)));

            if (nav && Frame.Content is FrameworkElement element)
            {
                InitialiseView(Frame.Content, context, viewModel);
                //element.SetContext(context);
                //if (viewModel is not null)
                //{
                //    element.DataContext = viewModel;
                //}
            }

            //return Frame.Content;
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
