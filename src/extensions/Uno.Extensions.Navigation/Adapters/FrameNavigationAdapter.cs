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
    public record AdapterFactory<TControl, TAdapter>(IServiceProvider Services) : IAdapterFactory
        where TAdapter : INavigationAdapter
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

        public override NavigationContext CurrentContext
        {
            get
            {
                return NavigationContexts.LastOrDefault();
            }
            protected set { }
        }

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

        protected override async Task DoBackNavigation(NavigationContext context)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 1);

            var currentVM = await CurrentContext.InitializeViewModel();

            ControlWrapper.Navigate(context, true, currentVM);

            await ((currentVM as INavigationStart)?.Start(CurrentContext, false) ?? Task.CompletedTask);

        }

        public override bool CanGoBack => NavigationContexts.Count > 1;

        protected override void AdapterNavigation(NavigationContext context, object viewModel)
        {
            NavigationContexts.Add(context);
            Frame.Navigate(context, false, viewModel);

            if (context.PathIsRooted)
            {
                while (NavigationContexts.Count > 1)
                {
                    NavigationContexts.RemoveAt(0);
                }

                Frame.ClearBackStack();
            }

            if (context.FramesToRemove > 0)
            {
                NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
                Frame.RemoveLastFromBackStack();
            }
        }
    }
}
