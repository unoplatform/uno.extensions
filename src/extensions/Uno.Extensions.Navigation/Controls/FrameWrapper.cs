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

public class FrameWrapper : BaseWrapper, IFrameWrapper<Frame>
{
    private Frame Frame => Control as Frame;

    public override void Inject(object control)
    {
        base.Inject(control);
        if (Frame.Content is not null)
        {
            Navigation.NavigateToView(null, Frame.SourcePageType);
        }
        Frame.Navigated += Frame_Navigated;
    }

    private INavigationService Navigation { get; }

    public FrameWrapper(INavigationService navigation)
    {
        Navigation = navigation;
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        Navigation.NavigateToView(null, Frame.SourcePageType);
    }

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

        if (context.Request.Sender is not null)
        {
            Frame.Navigated -= Frame_Navigated;
            var nav = Frame.Navigate(context.Mapping.View, context.Data);
            Frame.Navigated += Frame_Navigated;
        }

        if (Frame.Content is FrameworkElement element)
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
