using Uno.Extensions.Navigation.Regions;
using Windows.UI.Popups;

namespace Uno.Extensions.Navigation.Navigators;

public class MessageDialogNavigator : DialogNavigator
{
	public MessageDialogNavigator(
		ILogger<DialogNavigator> logger,
		IRouteResolver routeResolver,
		IRegion region)
		: base(logger, routeResolver, region)
	{
	}

	protected override bool QualifierIsSupported(Route route) =>
			base.QualifierIsSupported(route) ||
			// "-" (back or close) Add closing 
			route.IsBackOrCloseNavigation();

	protected override bool CanNavigateToRoute(Route route) =>
		base.CanNavigateToRoute(route) &&
		(RouteResolver.Find(route)?.View == typeof(MessageDialog));

	protected override async Task<IAsyncInfo?> DisplayDialog(NavigationRequest request, Type? viewType, object? viewModel)
	{
		var route = request.Route;
		var navigation = Region.Navigator();

		var data = route.Data;
		if (data is null)
		{
			return null;
		}

		var md = new MessageDialog(data[RouteConstants.MessageDialogParameterContent] as string, data[RouteConstants.MessageDialogParameterTitle] as string)
		{
			Options = (MessageDialogOptions)data[RouteConstants.MessageDialogParameterOptions],
			DefaultCommandIndex = (uint)data[RouteConstants.MessageDialogParameterDefaultCommand],
			CancelCommandIndex = (uint)data[RouteConstants.MessageDialogParameterCancelCommand]
		};
		md.Commands.AddRange(data[RouteConstants.MessageDialogParameterCommands] as UICommand[] ?? new UICommand[] { });
		var showTask = md.ShowAsync();
		showTask.AsTask()
			.ContinueWith(result =>
				{
					if (result.Status != TaskStatus.Canceled)
					{
						navigation?.NavigatePreviousWithResultAsync(request.Sender, data: Option.Some(result.Result));

					}
				},
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
				TaskScheduler.FromCurrentSynchronizationContext());

		if (request.Cancellation.HasValue &&
			request.Cancellation.Value.CanBeCanceled)
		{
			request.Cancellation.Value.Register(() =>
			{
				showTask.Cancel();
			});
		}

		return showTask;
	}
}
