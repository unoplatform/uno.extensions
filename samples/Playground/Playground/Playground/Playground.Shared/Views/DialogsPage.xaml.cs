namespace Playground.Views;

public sealed partial class DialogsPage : Page, IInjectable<INavigator>
{

	private INavigator? Navigator { get; set; }

	public DialogsPage()
	{
		this.InitializeComponent();
	}

	private async void MessageDialogCodebehindClick(object sender, RoutedEventArgs args)
	{
		var messageDialogResult = await Navigator!.ShowMessageDialogAsync<string>(this, content: "This is Content", title:"This is title");
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult}";
	}

	private async void MessageDialogCodebehindRouteClick(object sender, RoutedEventArgs args)
	{
		var messageDialogResult = await Navigator!.ShowMessageDialogAsync<string>(this, route:"LocalizedConfirm");
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult}";
	}
private async void MessageDialogCodebehindRouteOverrideClick(object sender, RoutedEventArgs args)
	{
		var messageDialogResult = await Navigator!.ShowMessageDialogAsync<string>(this, route:"LocalizedConfirm", content:"Override content", title:"Override title");
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult}";
	}
	private async void MessageDialogCodebehindCancelClick(object sender, RoutedEventArgs args)
	{
		var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var messageDialogResult = await Navigator!.ShowMessageDialogAsync<string>(this, content:"This is Content", title:"This is title", cancellation: cancelSource.Token);
		MessageDialogCancelResultText.Text = $"Message dialog result: {messageDialogResult}";
	}

	private async void SimpleDialogCodebehindClick(object sender, RoutedEventArgs args)
	{
		var dialogResult = await Task.Run(async () =>
		{
			return await Navigator!.NavigateViewForResultAsync<SimpleDialog, Widget>(this, Qualifiers.Dialog).AsResult();
		});
		SimpleDialogResultText.Text = $"Dialog result: {dialogResult.SomeOrDefault()?.ToString()}";
	}
	private async void SimpleDialogCodebehindCancelClick(object sender, RoutedEventArgs args)
	{
		var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var showDialog = await Navigator!.NavigateViewForResultAsync<SimpleDialog, Widget>(this, Qualifiers.Dialog, cancellation: cancelSource.Token);
		if (showDialog is null)
		{
			return;
		}
		var dialogResult = await showDialog.Result;
		SimpleDialogResultText.Text = $"Dialog result: {dialogResult.SomeOrDefault()?.ToString()}";
	}
	public void Inject(INavigator entity) => Navigator = entity;

	private async void FlyoutFromBackgroundClick(object sender, RoutedEventArgs e)
	{
		var result = await Task.Run(async () =>
		{
			// Note: Passing object in as sender to make sure navigation doesn't use the sender when showing flyout
			return await Navigator!.NavigateRouteAsync(new object(), "!Basic");
		});

	}

	private async void FlyoutFromBackgroundRequestingDataClick(object sender, RoutedEventArgs e)
	{
		var result = await Task.Run(async () =>
		{
			// Note: Passing object in as sender to make sure navigation doesn't use the sender when showing flyout
			return await Navigator!.NavigateRouteForResultAsync<Widget>(new object(), "!Basic").AsResult();
		});

	}
}
