using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
using Microsoft.Extensions.DependencyInjection;
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
    public interface IAdapterFactory
    {
        Type ControlType{get;}
        INavigationAdapter Create();
    }

    public record AdapterFactory<TControl, TAdapter>(IServiceProvider Services): IAdapterFactory
        where TAdapter: INavigationAdapter
    {
        public Type ControlType => typeof(TControl);

        public INavigationAdapter Create()
        {
            var scope = Services.CreateScope();
            var services = scope.ServiceProvider;
            return services.GetService<TAdapter>();
        }
    }

    public class FrameNavigationAdapter : BaseNavigationAdapter<Frame>
    {
        private IFrameWrapper Frame => ControlWrapper as IFrameWrapper;
        protected IList<NavigationContext> NavigationContexts { get; } = new List<NavigationContext>();

        public override NavigationContext CurrentContext {
            get
            {
                return NavigationContexts.LastOrDefault();
            }
            protected set { } } 

        public FrameNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IFrameWrapper frameWrapper) : base(services, navigationMapping, frameWrapper)
        { }

        protected override void PreNavigation(NavigationContext context)
        {

            var numberOfPagesToRemove = context.FramesToRemove;
            bool removeCurrentPageFromBackStack = numberOfPagesToRemove > 0;
            if (!context.IsBackNavigation && removeCurrentPageFromBackStack)
            {
                numberOfPagesToRemove--;
            }

            while (numberOfPagesToRemove > 0)
            {
                NavigationContexts.RemoveAt(NavigationContexts.Count - (context.IsBackNavigation ? 1 : 2));
                Frame.RemoveLastFromBackStack();
                numberOfPagesToRemove--;
            }
        }

        //protected override async Task<NavigationContext> AdapterNavigate(NavigationContext context, bool navBackRequired)
        //{
        //    var request = context.Request;
        //    var path = context.Path;

        //    var numberOfPagesToRemove = context.FramesToRemove;
        //    bool removeCurrentPageFromBackStack = numberOfPagesToRemove > 0;
        //    if (!context.IsBackNavigation && removeCurrentPageFromBackStack)
        //    {
        //        numberOfPagesToRemove--;
        //    }

        //    while (numberOfPagesToRemove > 0)
        //    {
        //        NavigationContexts.RemoveAt(NavigationContexts.Count - (context.IsBackNavigation ? 1 : 2));
        //        Frame.RemoveLastFromBackStack();
        //        numberOfPagesToRemove--;
        //    }

        //    if (context.Path == PreviousViewUri)
        //    {
        //        if (navBackRequired)
        //        {
        //            // Remove the last context (i.e the current context)
        //            NavigationContexts.RemoveAt(NavigationContexts.Count - 1);
        //        }

        //        var vm = await InitializeViewModel(CurrentContext);

        //        if (navBackRequired)
        //        {
        //            Frame.Navigate(context, vm);

        //            //var view = Frame.GoBack(context.Data, vm);
        //            //if (view is INavigationAware navAware)
        //            //{
        //            //    navAware.Navigation = Navigation;
        //            //}
        //        }

        //        await ((vm as INavigationStart)?.Start(CurrentContext, false) ?? Task.CompletedTask);
        //    }
        //    else
        //    {
        //        await DoForwardNavigation(context, (ctx, vm) =>
        //         {
        //             NavigationContexts.Add(ctx);
        //             Frame.Navigate(ctx, vm);
        //             //var view = Frame.Navigate(ctx, ctx.Mapping.View, ctx.Data, vm);
        //             //if (view is INavigationAware navAware)
        //             //{
        //             //    navAware.Navigation = Navigation;
        //             //}

        //             if (ctx.PathIsRooted)
        //             {
        //                 while (NavigationContexts.Count > 1)
        //                 {
        //                     NavigationContexts.RemoveAt(0);
        //                 }

        //                 Frame.ClearBackStack();
        //             }

        //             if (removeCurrentPageFromBackStack)
        //             {
        //                 NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
        //                 Frame.RemoveLastFromBackStack();
        //             }
        //         });
        //    }

        //    return context;
        //}

        protected override async Task<object> InitializeViewModel(NavigationContext context, bool navBackRequired)
        {

            if (navBackRequired)
            {
                // Remove the last context (i.e the current context)
                NavigationContexts.RemoveAt(NavigationContexts.Count - 1);
            }

            var vm = await base.InitializeViewModel(navBackRequired?CurrentContext:context, navBackRequired);

            if (navBackRequired)
            {
                Frame.Navigate(context, navBackRequired, vm);
            }

            return vm;
        }


        protected override void AdapterNavigation(NavigationContext context, object viewModel)
        {
            NavigationContexts.Add(context);
            Frame.Navigate(context, false, viewModel);
            //var view = Frame.Navigate(ctx, ctx.Mapping.View, ctx.Data, vm);
            //if (view is INavigationAware navAware)
            //{
            //    navAware.Navigation = Navigation;
            //}

            if (context.PathIsRooted)
            {
                while (NavigationContexts.Count > 1)
                {
                    NavigationContexts.RemoveAt(0);
                }

                Frame.ClearBackStack();
            }

            if (context.FramesToRemove>0)
            {
                NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
                Frame.RemoveLastFromBackStack();
            }
        }
    }
}
