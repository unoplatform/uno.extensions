using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public class FrameNavigationAdapter : BaseNavigationAdapter<Frame>
    {
        private IFrameWrapper Frame => ControlWrapper as IFrameWrapper;

        public FrameNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IFrameWrapper frameWrapper) : base(services, navigationMapping, frameWrapper)
        { }

        protected override async Task<NavigationContext> AdapterNavigate(NavigationContext context, bool navBackRequired)
        {
            var request = context.Request;
            var path = context.Path;

            var numberOfPagesToRemove = context.FramesToRemove;
            bool removeCurrentPageFromBackStack = numberOfPagesToRemove > 0;
            if (context.Path != PreviousViewUri && removeCurrentPageFromBackStack)
            {
                numberOfPagesToRemove--;
            }

            while (numberOfPagesToRemove > 0)
            {
                NavigationContexts.RemoveAt(NavigationContexts.Count - 1);
                Frame.RemoveLastFromBackStack();
                numberOfPagesToRemove--;
            }

            if (context.Path == PreviousViewUri)
            {
                var vm = await InitializeViewModel();

                if (navBackRequired)
                {
                    var view = Frame.GoBack(context.Data, vm);
                    if (view is INavigationAware navAware)
                    {
                        navAware.Navigation = Navigation;
                    }
                }

                await ((vm as INavigationStart)?.Start(NavigationContexts.Peek().Item2, false) ?? Task.CompletedTask);
            }
            else
            {
                await DoForwardNavigation(context, (ctx, vm) =>
                 {
                     var view = Frame.Navigate(ctx.Mapping.View, ctx.Data, vm);
                     if (view is INavigationAware navAware)
                     {
                         navAware.Navigation = Navigation;
                     }

                     if (ctx.PathIsRooted)
                     {
                         while (NavigationContexts.Count > 1)
                         {
                             NavigationContexts.RemoveAt(0);
                         }

                         Frame.ClearBackStack();
                     }

                     if (removeCurrentPageFromBackStack)
                     {
                         NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
                         Frame.RemoveLastFromBackStack();
                     }
                 });
            }

            return context;
        }
    }

    public static class ListHelpers
    {
        public static T Peek<T>(this IList<T> list)
        {
            var t = list.Last();
            return t;
        }

        public static T Pop<T>(this IList<T> list)
        {
            var t = list.Last();
            list.RemoveAt(list.Count - 1);
            return t;
        }

        public static void Push<T>(this IList<T> list, T item)
        {
            list.Add(item);
        }
    }
}
