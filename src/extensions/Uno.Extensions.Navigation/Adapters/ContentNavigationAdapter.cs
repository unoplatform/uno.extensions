using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public class ContentNavigationAdapter : BaseNavigationAdapter<ContentControl>
    {
        private IContentWrapper ContentHost => ControlWrapper as IContentWrapper;

        public ContentNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IContentWrapper contentWrapper) : base(services, navigationMapping, contentWrapper)
        {
        }

        protected override async Task<NavigationContext> AdapterNavigate(NavigationContext context, bool navBackRequired)
        {
            var request = context.Request;
            var path = context.Path;

            if (context.Path == PreviousViewUri)
            {
                var currentVM = await InitializeViewModel(CurrentContext);

                await ((currentVM as INavigationStart)?.Start(CurrentContext, false) ?? Task.CompletedTask);
            }
            else
            {
                await DoForwardNavigation(context, (ctx, vm) =>
                {
                    var view = ContentHost.ShowContent(ctx, ctx.Mapping.View, vm);

                    if (view is INavigationAware navAware)
                    {
                        navAware.Navigation = Navigation;
                    }

                    //// Content control can only show one view at a time, so remove
                    //// any old contexts
                    //if (NavigationContexts.Count > 1)
                    //{
                    //    NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
                    //}
                });
            }

            return context with { CanCancel = false };
        }
    }
}
