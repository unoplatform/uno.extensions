﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions;

public sealed class NavigationRegion : IRegion
{
    public string Name { get; private set; }

    public FrameworkElement View { get; }

    private IServiceProvider _services;
    private IRegion _parent;

    public IRegion Parent
    {
        get => _parent;
        private set
        {
            if (_parent is not null)
            {
                Parent.Children.Remove(this);
            }
            _parent = value;
            if (_parent is not null)
            {
                Parent.Children.Add(this);
            }
        }
    }

    public IServiceProvider Services
    {
        get
        {
            if (_services is null)
            {
                _services = Parent.Services.CreateScope().ServiceProvider;
                _services.AddInstance<IRegion>(this);
                var serviceFactory = _services.GetService<INavigatorFactory>();
                _services.AddInstance<INavigator>(() => serviceFactory.CreateService(this));
            }

            return _services;
        }
    }

    public ICollection<IRegion> Children { get; } = new List<IRegion>();

    public NavigationRegion(FrameworkElement view, IServiceProvider services = null)
    {
        View = view;
        if (view is not null)
        {
            View.Loading += ViewLoading;
            View.Loaded += ViewLoaded;
        }

        if (services is not null)
        {
            _services = services;
            _services.AddInstance<IRegion>(this);
            var serviceFactory = _services.GetService<INavigatorFactory>();
            _services.AddInstance<INavigator>(() => serviceFactory.CreateService(this));
        }
    }

    private async void ViewLoaded(object sender, RoutedEventArgs e)
    {
        await HandleLoading();
    }

#if WINDOWS_UWP || WINUI || NETSTANDARD
    private async void ViewLoading(FrameworkElement sender, object args)
#else
    private async void ViewLoading(DependencyObject sender, object args)
#endif
    {
        await HandleLoading();
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
        View.Loading += ViewLoading;
        View.Loaded += ViewLoaded;
        View.Unloaded -= ViewUnloaded;

        Parent = null;
    }

    private Task HandleLoading()
    {
        if (Parent is null)
        {
            var parent = View.FindParentRegion(out var routeName);
            Name = routeName;
            if (parent is not null)
            {
                Parent = parent;
            }
        }

        return View.IsLoaded ? HandleLoaded() : Task.CompletedTask;
    }

    private async Task HandleLoaded()
    {
        View.Loading -= ViewLoading;
        View.Loaded -= ViewLoaded;
        View.Unloaded += ViewUnloaded;

        // Force the lookup (and creation) of the navigator
        // This is required to intercept control event such as
        // navigating forward/backward on frame, or switching tabs
        _ = this.Navigator();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        PrintAllRegions(sb, this);
        return sb.ToString();
    }

    private static void PrintAllRegions(StringBuilder builder, IRegion region)
    {
        if (!string.IsNullOrWhiteSpace(region.Name))
        {
            builder.Append($@"{region.Name}");
        }

        if (region.View is not null)
        {
            builder.Append($@"({region.View.GetType().Name})-");
        }

        var nav = region.Navigator();
        if (nav is not null)
        {
            builder.Append($"{nav.ToString()}");
        }

        if (region.Children.Any())
        {
            builder.Append(" [");
        }

        foreach (var child in region.Children)
        {
            PrintAllRegions(builder, child);
        }

        if (region.Children.Any())
        {
            builder.Append("]");
        }
    }
}
