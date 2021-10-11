using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
#endif

namespace Uno.Extensions.Navigation.Regions;

public class FrameRegion : StackRegion<Frame>
{
    protected override object CurrentView => Control.Content;

    protected override string CurrentPath => CurrentView?.NavigationPath(Mappings);

    public FrameRegion(
        ILogger<FrameRegion> logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        RegionControlProvider controlProvider) : base(logger, scopedServices, navigation, viewModelManager, mappings, controlProvider.RegionControl as Frame)
    {
        if (Control.Content is not null)
        {
            Logger.LazyLogDebug(() => $"Navigating to type '{Control.SourcePageType.Name}' (initial Content set on Frame)");
            UpdateCurrentView();
        }

        Control.Navigated += Frame_Navigated;
    }

    private void UpdateCurrentView()
    {
        var request = Mappings.LookupByView(Control.Content.GetType()).AsRequest(this);
        var context = request.BuildNavigationContext(ScopedServices);
        InitialiseView(context);
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        Logger.LazyLogDebug(() => $"Frame has navigated to page '{e.SourcePageType.Name}'");

        UpdateCurrentView();
    }

    protected override void GoBack(object parameter)
    {
        try
        {
            Control.Navigated -= Frame_Navigated;
            if (parameter is not null)
            {
                Logger.LazyLogDebug(() => $"Replacing last backstack item to inject parameter '{parameter.GetType().Name}'");
                // If a parameter is being sent back, we need to replace
                // the last frame on the backstack with one that has the correct
                // parameter value. This value can be extracted via the OnNavigatedTo method
                var entry = Control.BackStack.Last();
                var newEntry = new PageStackEntry(entry.SourcePageType, parameter, entry.NavigationTransitionInfo);
                Control.BackStack.Remove(entry);
                Control.BackStack.Add(newEntry);
            }

            Logger.LazyLogDebug(() => $"Invoking Frame.GoBack");
            Control.GoBack();
            Logger.LazyLogDebug(() => $"Frame.GoBack completed");
            Control.Navigated += Frame_Navigated;
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to go back to page - {ex.Message}");
        }
    }

    protected override void Show(string path, Type view, object data)
    {
        try
        {
            if (Control.Content?.GetType() != view)
            {
                Logger.LazyLogDebug(() => $"Invoking Frame.Navigate to type '{view.Name}'");
                Control.Navigated -= Frame_Navigated;
                var nav = Control.Navigate(view, data);
                Control.Navigated += Frame_Navigated;
                Logger.LazyLogDebug(() => $"Frame.Navigate completed");
            }
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to navigate to page - {ex.Message}");
        }
    }

    protected override void RemoveLastFromBackStack()
    {
        Logger.LazyLogDebug(() => $"Removing last item from backstack (current count = {Control.BackStack.Count})");
        Control.BackStack.RemoveAt(Control.BackStack.Count - 1);
        Logger.LazyLogDebug(() => $"Item removed from backstack");
    }

    protected override void ClearBackStack()
    {
        Logger.LazyLogDebug(() => $"Clearing backstack");
        Control.BackStack.Clear();
        Logger.LazyLogDebug(() => $"Backstack cleared");
    }
}
