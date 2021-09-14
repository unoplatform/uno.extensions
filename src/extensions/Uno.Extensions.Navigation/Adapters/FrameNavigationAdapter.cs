using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters;

public class FrameNavigationAdapter : BaseNavigationAdapter
{
    private IFrameWrapper Frame => ControlWrapper as IFrameWrapper;

    protected IList<NavigationContext> NavigationContexts { get; } = new List<NavigationContext>();

    public override NavigationContext CurrentContext
    {
        get => NavigationContexts.LastOrDefault();
        protected set { }
    }

    public FrameNavigationAdapter(
        // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
        IServiceProvider services,
        IFrameWrapper frameWrapper) : base(services, frameWrapper)
    {
    }

    protected override async Task DoBackNavigation(NavigationContext context)
    {
        // Remove any excess items in the back stack
        var numberOfPagesToRemove = context.FramesToRemove;
        while (numberOfPagesToRemove > 0)
        {
            // Don't remove the last context, as that's the current page
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            Frame.RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Now remove the current context
        NavigationContexts.RemoveAt(NavigationContexts.Count - 1);

        // Initialise the view model for the previous context (which is now the current context)
        var currentVM = await CurrentContext.InitializeViewModel();

        // Invoke the navigation (which will be a back navigation)
        ControlWrapper.Navigate(context, true, currentVM);

        // Start the view model for the current
        await CurrentContext.StartViewModel(currentVM);
    }

    public override bool CanGoBack => NavigationContexts.Count > 1;

    protected override void AdapterNavigation(NavigationContext context, object viewModel)
    {
        var numberOfPagesToRemove = context.FramesToRemove;
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            Frame.RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Add the new context to the list of contexts and then navigate away
        NavigationContexts.Add(context);
        Frame.Navigate(context, false, viewModel);

        // If path starts with / then remove all prior pages and corresponding contexts
        if (context.PathIsRooted)
        {
            while (NavigationContexts.Count > 1)
            {
                NavigationContexts.RemoveAt(0);
            }

            Frame.ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (context.FramesToRemove > 0)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            Frame.RemoveLastFromBackStack();
        }
    }
}
