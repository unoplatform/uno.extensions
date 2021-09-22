using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation.Regions.Managers;

public class StackRegionManager<TControl> : BaseRegionManager
    where TControl : IStackViewManager
{
    private IStackViewManager StackControl { get; }

    private IList<NavigationContext> NavigationContexts { get; } = new List<NavigationContext>();

    public override NavigationContext CurrentContext => NavigationContexts.LastOrDefault();

    public StackRegionManager(ILogger<StackRegionManager<TControl>> logger, INavigationService navigation, IDialogFactory dialogFactory, TControl control) : base(logger, navigation, dialogFactory)
    {
        StackControl = control;
    }

    protected override async Task<object> DoNavigation(NavigationContext context)
    {
        if (context.IsBackNavigation)
        {
            return await DoBackNavigation(context);
        }
        else
        {
            return await DoForwardNavigation(context);
        }
    }

    protected async Task<object> DoBackNavigation(NavigationContext context)
    {
        // Remove any excess items in the back stack
        var numberOfPagesToRemove = context.FramesToRemove;
        while (numberOfPagesToRemove > 0)
        {
            // Don't remove the last context, as that's the current page
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            StackControl.RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Now remove the current context
        NavigationContexts.RemoveAt(NavigationContexts.Count - 1);

        // Initialise the view model for the previous context (which is now the current context)
        var currentVM = await CurrentContext.InitializeViewModel(Navigation);

        // Invoke the navigation (which will be a back navigation)
        StackControl.GoBack(context.Data, currentVM);

        return currentVM;
    }

    protected override bool CanGoBack => NavigationContexts.Count > 1;

    protected override void RegionNavigate(NavigationContext context, object viewModel)
    {
        var numberOfPagesToRemove = context.FramesToRemove;
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            StackControl.RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Add the new context to the list of contexts and then navigate away
        NavigationContexts.Add(context);
        StackControl.Show(context.Path, context.Mapping?.View, context.Data, viewModel);

        // If path starts with / then remove all prior pages and corresponding contexts
        if (context.PathIsRooted)
        {
            while (NavigationContexts.Count > 1)
            {
                NavigationContexts.RemoveAt(0);
            }

            StackControl.ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (context.FramesToRemove > 0)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            StackControl.RemoveLastFromBackStack();
        }
    }

    public override string ToString()
    {
        return $"Stack({typeof(TControl).Name}) '{CurrentContext?.Path}'";
    }
}
