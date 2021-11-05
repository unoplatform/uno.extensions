using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Options;
using Windows.UI;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;
#endif

namespace ExtensionsSampleApp.Navigators;

public class PickerNavigator : ControlNavigator
{
    public PickerNavigator(
        ILogger<ContentDialogNavigator> logger,
        IRouteMappings mappings,
        IRegion region)
        : base(logger, mappings, region)
    {
    }

    protected override Task<Route> RouteNavigateAsync(Route route)
    {
#if __IOS__
        var appWindow = Windows.UI.Xaml.Window.Current;
        var rootGrid = ((appWindow.Content is Frame frame) ? (frame.Content as Page).Content : appWindow.Content) as Grid;
        if (rootGrid is null)
        {
            return Task.FromResult<Route>(default);
        }

        var popup = new Popup() { IsLightDismissEnabled = true, VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
        var b = new Border()
        {
            Height = 320,
            Background = Colors.Beige
        };
        popup.Child = b;
        var picker = new Picker()
        {
            Height = 320,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var data = route.Data;
        if (data.TryGetValue(RouteConstants.PickerItemTemplate, out var template))
        {
            picker.ItemTemplate = template as DataTemplate;
        }
        if (data.TryGetValue(RouteConstants.PickerItemsSource, out var items))
        {
            picker.ItemsSource = items;
        }

        b.Add(picker);

        Grid.SetRowSpan(popup, rootGrid.RowDefinitions.Count > 0 ? rootGrid.RowDefinitions.Count : 1);
        Grid.SetColumnSpan(popup, rootGrid.ColumnDefinitions.Count > 0 ? rootGrid.ColumnDefinitions.Count : 1);
        rootGrid.Children.Add(popup);
        popup.IsOpen = true;

        picker.SelectionChanged += (p, e) =>
        {
            var navigation = Region.Navigator();
            var responseNav = navigation as ResponseNavigator;
            if (responseNav is not null)
            {
                if (e.AddedItems.Any())
                {
                    responseNav.ResultCompletion.TrySetResult(Option.Some(e.AddedItems.FirstOrDefault()));
                }
                else
                {
                    responseNav.ResultCompletion.TrySetResult(Option.None<object>());
                }
            }
            rootGrid.Children.Remove(popup);
        };

        popup.Closed += (pops, pope) =>
        {
            var navigation = Region.Navigator();
            var responseNav = navigation as ResponseNavigator;

            if (responseNav is not null)
            {
                responseNav.ResultCompletion.TrySetResult(Option.None<object>());
            }
            rootGrid.Children.Remove(popup);
        };
#endif
        var responseRoute = route with { Path = null };
        return Task.FromResult(responseRoute);
    }
}
