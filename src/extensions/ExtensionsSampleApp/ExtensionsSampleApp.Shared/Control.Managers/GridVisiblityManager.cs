﻿using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Control.Managers
{
    public class GridVisiblityManager : BaseControlManager<Grid>
    {
        public GridVisiblityManager(ILogger<GridVisiblityManager> logger, INavigationService navigation, RegionControlProvider controlProvider) : base(logger, navigation, controlProvider.RegionControl as Grid)
        {
        }

        private FrameworkElement CurrentlyVisibleControl { get; set; }
        protected override object InternalShow(string path, Type view, object data)
        {
            var controlToShow = Control.FindName(path) as FrameworkElement;
            if(controlToShow is null)
            {
                return null;
            }

            controlToShow.Visibility = Visibility.Visible;

            if(CurrentlyVisibleControl != null)
            {
                CurrentlyVisibleControl.Visibility = Visibility.Collapsed;
            }
            CurrentlyVisibleControl = controlToShow;
            return controlToShow;
        }
    }
}
