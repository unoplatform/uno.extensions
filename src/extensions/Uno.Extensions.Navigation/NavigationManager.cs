﻿using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Navigation.Adapters;

namespace Uno.Extensions.Navigation
{
    public class NavigationManager : INavigationManager
    {
        private INavigationService Root { get; set; }

        private IDictionary<Type, IAdapterFactory> Factories { get; }

        public NavigationManager(IEnumerable<IAdapterFactory> factories)
        {
            Factories = factories.ToDictionary(x => x.ControlType);
        }

        public INavigationService AddAdapter(INavigationService parentAdapter, string routeName, object control, INavigationService existingAdapter)
        {
            var ans = existingAdapter as NavigationService;
            var parent = parentAdapter as NavigationService;
            if (ans is null)
            {
                var factory = Factories[control.GetType()];

                var adapter = factory.Create();
                adapter.Name = routeName;
                adapter.Inject(control);

                ans = new NavigationService(this, adapter, parent);
            }

            if (parent is null)
            {
#if DEBUG
                if (Root is not null)
                {
                    throw new Exception("Null root adapter expected");
                }
#endif
                Root = ans;
            }
            else
            {
                parent.NestedAdapters[routeName + string.Empty] = ans;
            }

            if (ans.Adapter is INavigationAware navAware)
            {
                navAware.Navigation = ans;
            }
            return ans;
        }

        public void RemoveAdapter(INavigationService adapter)
        {
            var ans = adapter as NavigationService;
            if (ans is null)
            {
                return;
            }

            // Detach adapter from parent
            var parent = adapter.Parent as NavigationService;
            if (parent is not null)
            {
                parent.NestedAdapters.Remove(kvp => kvp.Value == adapter);
            }
        }

        public INavigationService Parent => Root;

        public NavigationResponse Navigate(NavigationRequest request)
        {
            return Root.Navigate(request);
        }

        public INavigationService Nested(string routeName = null)
        {
            return Root.Nested(routeName);
        }
    }
}
