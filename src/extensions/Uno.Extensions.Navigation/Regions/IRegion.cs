using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation.Regions
{
    public interface IRegion
    {
        void UpdateServiceProvider(IServiceProvider services);

        IRegion Parent { get; set; }

        void Attach(IRegion childRegion, string regionName);

        void Detach(IRegion childRegion);

        Task<IEnumerable<IRegion>> FindChildByRegionName(string regionName);

        INavigationService Navigation { get; }

        IRegionNavigationServiceFactory NavigationFactory { get; }
    }

    public sealed class Region : IRegion
    {
        public IRegion Parent { get; set; }

        private IScopedServiceProvider Services { get; set; }

        private IList<(string, IRegion)> Children { get; } = new List<(string, IRegion)>();

        private AsyncAutoResetEvent NestedServiceWaiter { get; } = new AsyncAutoResetEvent(false);

        public void Attach(IRegion childRegion, string regionName)
        {
            var childService = childRegion;
            Children.Add((regionName + string.Empty, childService));
            childRegion.Parent = this;
            NestedServiceWaiter.Set();
        }

        public void Detach(IRegion childRegion)
        {
            childRegion.Parent = null;
            Children.Remove(kvp => kvp.Item2 == childRegion);
        }

        public async Task<IEnumerable<IRegion>> FindChildByRegionName(string regionName)
        {
            Func<IRegion[]> find = () => Children.Where(kvp => string.IsNullOrWhiteSpace(kvp.Item1)).Select(x => x.Item2).ToArray();

            var matched = find();
            while (!matched.Any())
            {
                await NestedServiceWaiter.Wait();
                matched = find();
            }

            return matched;
        }

        public void UpdateServiceProvider(IServiceProvider services)
        {
            Services = new ScopedServiceProvider(services.CreateScope().ServiceProvider);

            foreach (var child in Children)
            {
                child.Item2.UpdateServiceProvider(services);
            }
        }

        public void AttachAll(IEnumerable<(string, IRegion)> children)
        {
            children.ForEach(n => Attach(n.Value.Item2, n.Value.Item1));
        }

        public IEnumerable<(string, IRegion)> DetachAll()
        {
            var children = Children.ToArray();
            Children.ForEach(child => Detach(child.Value.Item2));
            return children;
        }

        public INavigationService Navigation => Services.GetService<IRegionNavigationService>();

        public IRegionNavigationServiceFactory NavigationFactory => Services.GetService<IRegionNavigationServiceFactory>();
    }
}
