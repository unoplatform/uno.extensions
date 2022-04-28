namespace Uno.Extensions.Navigation.Navigators;

public class MessageDialogNavigator : DialogNavigator
{
	public MessageDialogNavigator(
		ILogger<DialogNavigator> logger,
		IDispatcher dispatcher,
		IRouteResolver resolver,
		IRegion region,
		Window window)
		: base(logger, dispatcher, region, resolver, window)
	{
	}

	protected override bool RegionCanNavigate(Route route, RouteInfo? routeMap) =>
		base.RegionCanNavigate(route, routeMap) &&
		(routeMap?.RenderView == typeof(MessageDialog));

	protected override async Task<IAsyncInfo?> DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel)
	{
		var route = request.Route;
		var navigation = Region.Navigator();

		var data = route.Data;
		if (data is null)
		{
			return null;
		}

		var messageView = Resolver.Find(route)?.ViewAttributes as MessageDialogAttributes;

		var content = data.TryGetValue(RouteConstants.MessageDialogParameterContent, out var contentValue) ?
						contentValue as string :
						messageView?.Content;
		var title = data.TryGetValue(RouteConstants.MessageDialogParameterTitle, out var titleValue) ?
						titleValue as string :
						messageView?.Title;
		var options = data.TryGetValue(RouteConstants.MessageDialogParameterOptions, out var optionsValue) ?
						(optionsValue is MessageDialogOptions opt) ? opt : MessageDialogOptions.None :
						(messageView?.DelayUserInput ?? false) ? MessageDialogOptions.AcceptUserInputAfterDelay : MessageDialogOptions.None;
		var defaultIndex = (uint)(data.TryGetValue(RouteConstants.MessageDialogParameterDefaultCommand, out var defaultValue) ?
						(defaultValue is int defIdx) ? defIdx : 0 :
						messageView?.DefaultButtonIndex ?? 0);
		var cancelIndex = (uint)(data.TryGetValue(RouteConstants.MessageDialogParameterCancelCommand, out var cancelValue) ?
						(cancelValue is int cancel) ? cancel : 0 :
						messageView?.CancelButtonIndex ?? 0);
		var buttons = (data.TryGetValue(RouteConstants.MessageDialogParameterCommands, out var buttonValues) ?
						buttonValues as DialogAction[] :
						messageView?.Buttons) ?? new DialogAction[] { };

		var commands = from b in buttons
					   select new UICommand(b.Label, new UICommandInvokedHandler(cmd => b.Action?.Invoke()), b.Id);

		var md = new MessageDialog(
			content,
			title
			)
		{
			Options = options,
			DefaultCommandIndex = defaultIndex,
			CancelCommandIndex = cancelIndex
		};
		md.Commands.AddRange(commands);

#if WINUI && WINDOWS
		var window = Window!;
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
		WinRT.Interop.InitializeWithWindow.Initialize(md, hwnd);
#endif

		var showTask = md.ShowAsync();
		_ = showTask.AsTask()
			.ContinueWith(result =>
				{
					if (result.Status != TaskStatus.Canceled)
					{
						var msgResult = result.Result;
						var dialogAction = msgResult is not null ?
											(buttons.FirstOrDefault(b => b.Label == msgResult.Label) ?? new DialogAction(Label: msgResult.Label, Id: msgResult.Id)) :
											default;
						if (dialogAction is not null)
						{
							navigation?.NavigateBackWithResultAsync(request.Sender, data: Option.Some(dialogAction));
						}
						else
						{
							navigation?.NavigateBackWithResultAsync(request.Sender, data: Option.None<DialogAction>());
						}

					}
				},
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
				TaskScheduler.FromCurrentSynchronizationContext());

		if (request.Cancellation.HasValue &&
			request.Cancellation.Value.CanBeCanceled)
		{
			request.Cancellation.Value.Register(async () =>
			{
				await this.Dispatcher.ExecuteAsync(() => showTask.Cancel());
			});
		}

		return showTask;
	}
}
