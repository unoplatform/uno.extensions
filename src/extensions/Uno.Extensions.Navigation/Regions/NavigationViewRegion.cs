﻿using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions;

public class NavigationViewRegion : SimpleRegion<Microsoft.UI.Xaml.Controls.NavigationView>
{
    protected override object CurrentView => Control.SelectedItem;

    private Microsoft.UI.Xaml.Controls.NavigationView _control;

    public override Microsoft.UI.Xaml.Controls.NavigationView Control
    {
        get => _control;
        set
        {
            if (_control != null)
            {
                _control.SelectionChanged -= ControlSelectionChanged;
            }
            _control = value;
            if (_control != null)
            {
                _control.SelectionChanged += ControlSelectionChanged;
            }
        }
    }

    private void ControlSelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        var tbi = args.SelectedItem as FrameworkElement;

        var path = Uno.Extensions.Navigation.Controls.Navigation.GetPath(tbi) ?? tbi.Name;
        if (!string.IsNullOrEmpty(path))
        {
            Navigation.NavigateByPathAsync(null, path);
        }
    }

    public NavigationViewRegion(
        ILogger<NavigationViewRegion> logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,

        RegionControlProvider controlProvider) : base(logger, scopedServices, navigation, viewModelManager, controlProvider.RegionControl as Microsoft.UI.Xaml.Controls.NavigationView)
    {
    }

    protected override void Show(string path, Type view, object data)
    {
        var item = (from mi in Control.MenuItems.OfType<FrameworkElement>()
                    where mi.Name == path
                    select mi).FirstOrDefault();
        if (item != null)
        {
            Control.SelectedItem = item;
        }
    }
}
