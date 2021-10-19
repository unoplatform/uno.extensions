using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

        public NavigationRegion(FrameworkElement view, IServiceProvider services = null)
        {
            View = view;
            if (view is not null)
            {
                View.Loading += ViewLoading;
            }

            if (services is not null)
            {
                _services = services;
                _services.AddInstance<IRegion>(this);
                var serviceFactory = _services.GetService<INavigatorFactory>();
                _services.AddInstance<INavigator>(() => serviceFactory.CreateService(this));
            }
        }

        public NavigationRegion(FrameworkElement view, IRegion parent)
        {
            View = view;
            if (view is not null)
            {
                View.Loading += ViewLoading;
            }

            Parent = parent;
        }

        private void ViewLoading(FrameworkElement sender, object args)
        {
            var parent = sender.FindParentRegion(out var routeName);
            Name = routeName;
            if (parent is not null)
            {
                Parent = parent;
            }

            sender.Unloaded += (sUnloaded, eUnloaded) =>
            {
                if (parent is not null)
                {
                    parent.Detach(this);
                }
            };
        }
    }
}
