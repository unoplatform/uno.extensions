using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Windows.Foundation;
using System.Threading;
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
    public abstract class BaseNavigationAdapter<TControl> : INavigationAdapter
    {
        public const string PreviousViewUri = "..";
        public const string MessageDialogUri = "__md__";
        public const string MessageDialogParameterContent = MessageDialogUri + "content";
        public const string MessageDialogParameterTitle = MessageDialogUri + "title";
        public const string MessageDialogParameterOptions = MessageDialogUri + "options";
        public const string MessageDialogParameterDefaultCommand = MessageDialogUri + "default";
        public const string MessageDialogParameterCancelCommand = MessageDialogUri + "cancel";
        public const string MessageDialogParameterCommands = MessageDialogUri + "commands";

        protected IControlNavigation ControlWrapper { get; }

        public virtual NavigationContext CurrentContext { get; protected set; }

        public string Name { get; set; }

        protected INavigationMapping Mapping { get; }

        protected IServiceProvider Services { get; }

        public INavigationService Navigation { get; set; }

        // protected IList<(string, NavigationContext)> NavigationContexts { get; } = new List<(string, NavigationContext)>();

        protected Stack<(IAsyncInfo, NavigationContext)> OpenDialogs { get; } = new Stack<(IAsyncInfo, NavigationContext)>();

        public void Inject(object control)
        {
            ControlWrapper.Inject(control);
        }

        public virtual bool IsCurrentPath(string path)
        {
            return CurrentContext.Path == path;
        }

        public BaseNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IControlNavigation control)
        {
            Services = services.CreateScope().ServiceProvider;
            Mapping = navigationMapping;
            ControlWrapper = control;
        }

        public NavigationResponse Navigate(NavigationContext context)
        {
            var request = context.Request;

            var navTask = InternalNavigate(context);

            return new NavigationResponse(request, navTask, context.CancellationSource, context.ResultCompletion.Task);
        }

        private async Task InternalNavigate(NavigationContext context)
        {
            var navBackRequired = await EndCurrentNavigationContext(context);

            if (context.CancellationToken.IsCancellationRequested)
            {
                await Task.FromCanceled(context.CancellationToken);
            }

            context = await AdapterNavigate(context, navBackRequired);

            if (context.CanCancel)
            {
                context.CancellationToken.Register(() =>
                {
                    Navigation.NavigateToPreviousView(context.Request.Sender);
                });
            }
        }

        protected async Task<NavigationContext> AdapterNavigate(NavigationContext context, bool navBackRequired)
        {
            var request = context.Request;
            var path = context.Path;

            PreNavigation(context);

            if (context.Path == PreviousViewUri)
            {
                var currentVM = await InitializeViewModel(CurrentContext, navBackRequired);

                await ((currentVM as INavigationStart)?.Start(CurrentContext, false) ?? Task.CompletedTask);
            }
            else
            {
                await DoForwardNavigation(context);
            }

            return context with { CanCancel = false };
        }

        protected async Task DoForwardNavigation(NavigationContext context)
        {
            var mapping = Mapping.LookupByPath(context.Path);
            if (mapping is not null)
            {
                context = context with { Mapping = mapping };
            }

            //// Push the new navigation context
            //NavigationContexts.Push((context.Path, context));

            var vm = await InitializeViewModel(context, false);

            var data = context.Data;
            if (context.Path == MessageDialogUri)
            {
                var md = new MessageDialog(data[MessageDialogParameterContent] as string, data[MessageDialogParameterTitle] as string)
                {
                    Options = (MessageDialogOptions)data[MessageDialogParameterOptions],
                    DefaultCommandIndex = (uint)data[MessageDialogParameterDefaultCommand],
                    CancelCommandIndex = (uint)data[MessageDialogParameterCancelCommand]
                };
                md.Commands.AddRange((data[MessageDialogParameterCommands] as UICommand[]) ?? new UICommand[] { });
                var showTask = md.ShowAsync();
                OpenDialogs.Push((showTask, context));
                showTask.AsTask().ContinueWith(result =>
                {
                    if (result.Status != TaskStatus.Canceled &&
                    context.ResultCompletion.Task.Status != TaskStatus.Canceled &&
                    context.ResultCompletion.Task.Status != TaskStatus.RanToCompletion)
                    {
                        Navigation.Navigate(new NavigationRequest(md, new NavigationRoute(new Uri(PreviousViewUri, UriKind.Relative), result.Result)));
                    }
                }, CancellationToken.None,
                                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                                TaskScheduler.FromCurrentSynchronizationContext());
            }
            else if (mapping?.View?.IsSubclassOf(typeof(ContentDialog)) ?? false)
            {
                var dialog = Activator.CreateInstance(mapping.View) as ContentDialog;
                if (vm is not null)
                {
                    dialog.DataContext = vm;
                }
                if (dialog is INavigationAware navAware)
                {
                    navAware.Navigation = Navigation;
                }

                var showTask = dialog.ShowAsync();
                OpenDialogs.Push((showTask, context));

                showTask.AsTask().ContinueWith(result =>
                                {
                                    if (result.Status != TaskStatus.Canceled &&
                                    context.ResultCompletion.Task.Status != TaskStatus.Canceled &&
                                    context.ResultCompletion.Task.Status != TaskStatus.RanToCompletion)
                                    {
                                        Navigation.Navigate(new NavigationRequest(dialog, new NavigationRoute(new Uri(PreviousViewUri, UriKind.Relative), result.Result)));
                                    }
                                }, CancellationToken.None,
                                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                                TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                AdapterNavigation(context, vm);
            }
            await ((vm as INavigationStart)?.Start(context, true) ?? Task.CompletedTask);
        }

        protected virtual void PreNavigation(NavigationContext context)
        {

        }
        protected virtual void AdapterNavigation(NavigationContext context, object viewModel)
        {
            CurrentContext = context;
            ControlWrapper.Navigate(context,false, viewModel);
        }

        protected async Task<bool> EndCurrentNavigationContext(NavigationContext context)
        {
            var frameNavigationRequired = true;
            // If there's a current nav context, make sure it's stopped before
            // we proceed - this could cancel the navigation, so need to know
            // before we remove anything from backstack
            if (CurrentContext is not null)
            {
                var currentVM = await StopCurrentViewModel(context);

                if (context.IsCancelled)
                {
                    return false;
                }

                if (context.Path == PreviousViewUri)
                {
                    var responseData = context.Data.TryGetValue(string.Empty, out var response) ? response : default;

                    var previousContext = CurrentContext;

                    if (OpenDialogs.Any())
                    {
                        var dialog = OpenDialogs.Pop();
                        var showTask = dialog.Item1;
                        previousContext = dialog.Item2;
                        if (showTask is IAsyncOperation<IUICommand> showMessageDialogTask)
                        {
                            frameNavigationRequired = false;
                            showMessageDialogTask.Cancel();
                        }

                        if (showTask is IAsyncOperation<ContentDialogResult> contentDialogTask)
                        {
                            frameNavigationRequired = false;
                            if (!(responseData is ContentDialogResult))
                            {
                                contentDialogTask.Cancel();
                            }

                            var resultType = previousContext.Request.Result;

                            if (resultType is not null && responseData is not null)
                            {
                                if (resultType == typeof(ContentDialogResult))
                                {
                                    if (responseData is not ContentDialogResult result)
                                    {
                                        responseData = ContentDialogResult.None;
                                    }
                                }
                                else if (resultType == typeof(ContentResult))
                                {
                                    if (responseData is ContentDialogResult result)
                                    {
                                        responseData = new ContentResult(result);
                                    }
                                    else
                                    {
                                        responseData = new ContentResult(ContentDialogResult.None, responseData);
                                    }
                                }
                            }

                        }
                    }
                    if (previousContext.Request.Result is not null)
                    {
                        var completion = previousContext.ResultCompletion;
                        if (completion is not null)
                        {
                            completion.SetResult(responseData);
                        }
                    }
                }

            }

            return frameNavigationRequired;
        }

        protected async Task<object> StopCurrentViewModel(NavigationContext navigation)
        {
            var context = CurrentContext;

            object oldVm = default;
            if (context.Mapping?.ViewModel is not null)
            {
                var services = context.Services;
                oldVm = services.GetService(context.Mapping.ViewModel);
                await ((oldVm as INavigationStop)?.Stop(navigation, false) ?? Task.CompletedTask);
            }
            return oldVm;
        }

        protected virtual async Task<object> InitializeViewModel(NavigationContext context, bool navBackRequired)
        {
            var mapping = context.Mapping;
            object vm = default;
            if (mapping?.ViewModel is not null)
            {
                var services = context.Services;
                var dataFactor = services.GetService<ViewModelDataProvider>();
                dataFactor.Parameters = context.Data;

                vm = services.GetService(mapping.ViewModel);
                await ((vm as IInitialise)?.Initialize(context) ?? Task.CompletedTask);
            }
            return vm;
        }
    }

    public record ContentResult (ContentDialogResult Result, object Data=null)
    {
        public static implicit operator ContentDialogResult(
                                       ContentResult entity)
        {
            return entity.Result;
        }
    }
}
