using Uno.Extensions.Navigation.Regions;
using Windows.UI.Popups;

namespace Uno.Extensions.Navigation.Navigators;

public class MessageDialogNavigator : DialogNavigator
{
	public MessageDialogNavigator(
		ILogger<DialogNavigator> logger,
		IResolver resolver,
		IRegion region)
		: base(logger, resolver, region)
	{
	}

	protected override bool QualifierIsSupported(Route route) =>
			base.QualifierIsSupported(route) ||
			// "-" (back or close) Add closing 
			route.IsBackOrCloseNavigation();

	protected override bool CanNavigateToRoute(Route route) =>
		!route.IsDialog() &&
		(Resolver.Routes.Find(route)?.View?.View == typeof(MessageDialog));

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
						navigation?.NavigateBackWithResultAsync(request.Sender, data: Option.Some(result.Result));

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
