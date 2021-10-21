using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions
{
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
                    Parent.Detach(this);
                }
                _parent = value;
                if (_parent is not null)
                {
                    Parent.Attach(this);
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

        public IList<IRegion> Children { get; } = new List<IRegion>();

        public NavigationRegion(FrameworkElement view, IServiceProvider services = null) : this(view)
        {
            if (services is not null)
            {
                _services = services;
                _services.AddInstance<IRegion>(this);
                var serviceFactory = _services.GetService<INavigatorFactory>();
                _services.AddInstance<INavigator>(() => serviceFactory.CreateService(this));
            }
        }



        public NavigationRegion(FrameworkElement view, IRegion parent) : this(view)
        {
            Parent = parent;
        }

        private NavigationRegion(FrameworkElement view)
        {
            View = view;
            if (view is not null)
            {
                View.Loading += ViewLoading;
                View.Loaded += ViewLoaded;
            }
        }

        private async void ViewLoaded(object sender, RoutedEventArgs e)
        {
            await HandleLoading();
        }

        private async void ViewLoading(FrameworkElement sender, object args)
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
        }
    }
}
