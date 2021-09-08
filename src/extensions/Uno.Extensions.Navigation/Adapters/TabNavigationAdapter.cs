using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public class TabNavigationAdapter : BaseNavigationAdapter<TabView>
    {
        private ITabWrapper Tabs => ControlWrapper as ITabWrapper;

        public TabNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            ITabWrapper tabWrapper) : base(services, navigationMapping, tabWrapper)
        {
        }

        protected override async Task<NavigationContext> AdapterNavigate(NavigationContext context, bool navBackRequired)
        {
            var request = context.Request;
            var path = context.Path;

            if (context.Path == PreviousViewUri)
            {
                var currentVM = await InitializeViewModel();

                await ((currentVM as INavigationStart)?.Start(NavigationContexts.Peek().Item2, false) ?? Task.CompletedTask);
            }
            else
            {
                await DoForwardNavigation(context, (ctx, vm) =>
                {
                    var view = Tabs.ActivateTab(ctx.Path, vm);

                    if (view is INavigationAware navAware)
                    {
                        navAware.Navigation = Navigation;
                    }
                });
            }

            return context with { CanCancel = false };
        }
    }
}
