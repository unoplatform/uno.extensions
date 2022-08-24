
namespace TestHarness.Ext.Navigation.Dialogs;
public sealed partial class ContentDialogsPage : Page
{
	public ContentDialogsPage()
	{
		this.InitializeComponent();
	}

	private async void SimpleDialogCodebehindClick(object sender, RoutedEventArgs args)
	{
		var nav = this.Navigator()!;
		var dialogResult = await Task.Run(async () =>
		{
			return await nav.NavigateViewForResultAsync<DialogsSimpleDialog, Widget>(this, Qualifiers.Dialog).AsResult();
		});
		if (dialogResult.Type == OptionType.Some)
		{
			SimpleDialogResultText.Text = $"Dialog result: {dialogResult.SomeOrDefault()?.ToString()}";
		}
	}
	private async void SimpleDialogCodebehindCancelClick(object sender, RoutedEventArgs args)
	{
		var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var showDialog = await this.Navigator()!.NavigateViewForResultAsync<DialogsSimpleDialog, Widget>(this, Qualifiers.Dialog, cancellation: cancelSource.Token);
		if (showDialog is null)
		{
			return;
		}
		var dialogResult = await showDialog.Result;
		if (dialogResult.Type == OptionType.Some)
		{
			SimpleDialogResultText.Text = $"Dialog result: {dialogResult.SomeOrDefault()?.ToString()}";
		}
	}
}
