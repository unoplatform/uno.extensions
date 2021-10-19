using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Uno.Extensions.Navigation.Controls;
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

        private IList<IRegion> Children { get; } = new List<IRegion>();

        private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

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

        public void Attach(IRegion childRegion)
        {
            Children.Add(childRegion);
            NestedServiceWaiter.Set();
        }

        public void Detach(IRegion childRegion)
        {
            Children.Remove(kvp => kvp.Name == childRegion.Name);
        }

        public async Task<IEnumerable<(IRegion, NavigationRequest)>> GetChildren(Func<IRegion, (IRegion, NavigationRequest)> predicate, bool blocking)
        {
            //return Children.Where(child => predicate(child)).ToArray(); 
            Func<(IRegion, NavigationRequest)[]> find = () => (from child in Children
                                          let match = predicate(child)
                                          where match != default((IRegion, NavigationRequest))
                                          select match).ToArray();
            var matched = find();
            while (!matched.Any())
            {
                if (!blocking)
                {
                    return null;
                }

                await NestedServiceWaiter.Wait();

                matched = find();
            }

            return matched;
        }

        public void AttachAll(IEnumerable<IRegion> children)
        {
            children.ForEach(n => Attach(n));
        }

        public IEnumerable<IRegion> DetachAll()
        {
            var children = Children.ToArray();
            children.ForEach(child => Detach(child));
            return children;
        }
    }
}
