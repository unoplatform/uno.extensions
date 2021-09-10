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

namespace Uno.Extensions.Navigation.Controls;

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
    }

    public void Navigate(NavigationContext context, bool isBackNavigation, object viewModel)
    {
        if (isBackNavigation)
        {
            GoBack(context, context.Data, viewModel);
            return;
        }
        var nav = Frame.Navigate(context.Mapping.View, context.Data);

        if (nav && Frame.Content is FrameworkElement element)
        {
            InitialiseView(Frame.Content, context, viewModel);
        }
    }

    public void RemoveLastFromBackStack()
    {
        Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
    }

    public void ClearBackStack()
    {
        Frame.BackStack.Clear();
    }
}
