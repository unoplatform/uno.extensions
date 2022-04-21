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
		var showDialog = await Navigator!.ShowMessageDialogAsync(this, "This is Content", "This is title");
		if(showDialog is null)
		{
			return;
		}
		var messageDialogResult = await showDialog.Result;
		MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult.SomeOrDefault()?.Label}";
	}

	private async void MessageDialogCodebehindCancelClick(object sender, RoutedEventArgs args)
	{
		var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var showDialog = await Navigator!.ShowMessageDialogAsync(this, "This is Content", "This is title", cancellation: cancelSource.Token);
		if (showDialog is null)
		{
			return;
		}
		var messageDialogResult = await showDialog.Result;
		MessageDialogCancelResultText.Text = $"Message dialog result: {messageDialogResult.SomeOrDefault()?.Label}";
	}

	private async void SimpleDialogCodebehindClick(object sender, RoutedEventArgs args)
	{
		var dialogResult = await Task.Run(async () =>
		{
			var showDialog = await Navigator!.NavigateViewForResultAsync<SimpleDialog, Widget>(this, Qualifiers.Dialog);
			if (showDialog is null)
			{
				return Option.None<Widget>();
			}
			return await showDialog.Result;
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
		var response = await Task.Run(async () =>
		{
			// Note: Passing object in as sender to make sure navigation doesn't use the sender when showing flyout
			return await Navigator.NavigateRouteForResultAsync<string>(new object(), "!Basic");
		});

		var result = await response.Result;
	}
}
