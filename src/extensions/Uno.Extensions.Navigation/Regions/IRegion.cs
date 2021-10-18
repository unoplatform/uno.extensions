using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions
{
    public interface IRegion
    {
        string Name { get; }

        FrameworkElement View { get; }

        IServiceProvider Services { get; }

        IRegion Parent { get; set; }

        void Attach(IRegion childRegion);

        void Detach(IRegion childRegion);

        Task<IEnumerable<IRegion>> GetChildren(string regionName);

        // TODO: Work out how we can remove these
        void AttachAll(IEnumerable<IRegion> children);
        IEnumerable<IRegion> DetachAll();
    }

    public static class RegionExtensions
    {
        public static INavigationService Navigation(this IRegion region) => region.Services?.GetService<INavigationService>();

        public static IRegionNavigationServiceFactory NavigationFactory(this IRegion region) => region.Services?.GetService<IRegionNavigationServiceFactory>();

        public static Task<NavigationResponse> NavigateAsync(this IRegion region, NavigationRequest request) => region.Navigation()?.NavigateAsync(request);

    }

    public sealed class NavigationRegion : IRegion
    {
        public string Name { get; }

        public FrameworkElement View { get; }

        private IServiceProvider _services;
        private IRegion _parent;

        public IRegion Parent
        {
            get => _parent;
            set
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
                    var serviceFactory = _services.GetService<IRegionNavigationServiceFactory>();
                    _services.AddInstance<INavigationService>(() => serviceFactory.CreateService(this));
                }

                return _services;
            }
        }

        private IList<IRegion> Children { get; } = new List<IRegion>();

        private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

        public NavigationRegion(string regionName, FrameworkElement view, IServiceProvider services = null)
        {
            Name = regionName;
            View = view;
            if (view is not null)
            {
                View.Loaded += ViewLoaded;
            }

            if (services is not null)
            {
                _services = services;
                _services.AddInstance<IRegion>(this);
                var serviceFactory = _services.GetService<IRegionNavigationServiceFactory>();
                _services.AddInstance<INavigationService>(() => serviceFactory.CreateService(this));
            }
        }

        public NavigationRegion(string regionName, FrameworkElement view, IRegion parent)
        {
            Name = regionName;
            View = view;
            if (view is not null)
            {
                View.Loaded += ViewLoaded;
            }

            Parent = parent;
        }

        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            var loadedElement = sender as FrameworkElement;
            var parent = loadedElement.Parent.Region();
            if (parent is not null)
            {
                Parent = parent;
            }

            loadedElement.Unloaded += (sUnloaded, eUnloaded) =>
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

        public async Task<IEnumerable<IRegion>> GetChildren(string regionName)
        {
            if (Services is null)
            {
                await NestedServiceWaiter.Wait();
            }

            Func<IRegion[]> findByName = () => Children.Where(kvp => kvp.Name == regionName).ToArray();
            Func<IRegion[]> findByNoName = () => Children.Where(kvp => string.IsNullOrWhiteSpace(kvp.Name)).ToArray();

            var matched = findByName();
            while (!matched.Any())
            {
                matched = findByNoName();
                if (matched.Any())
                {
                    break;
                }
                await NestedServiceWaiter.Wait();
                matched = findByName();
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
