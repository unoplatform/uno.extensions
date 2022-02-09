using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Playground.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class DialogsPage : Page, IInjectable<INavigator>
	{

		private INavigator Navigator { get; set; }

		public DialogsPage()
        {
            this.InitializeComponent();
        }
		
		private async void MessageDialogCodebehindClick(object sender, RoutedEventArgs args)
		{
			var showDialog = await Navigator.ShowMessageDialogAsync(this, "This is Content", "This is title");
			var messageDialogResult = await showDialog.Result;
			MessageDialogResultText.Text = $"Message dialog result: {messageDialogResult.SomeOrDefault()?.Label}";
		}

		private async void MessageDialogCodebehindCancelClick(object sender, RoutedEventArgs args)
		{
			var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
			var showDialog = await Navigator.ShowMessageDialogAsync(this, "This is Content", "This is title", cancellation: cancelSource.Token);
			var messageDialogResult = await showDialog.Result;
			MessageDialogCancelResultText.Text = $"Message dialog result: {messageDialogResult.SomeOrDefault()?.Label}";
		}

		private async void SimpleDialogCodebehindClick(object sender, RoutedEventArgs args)
		{
			var showDialog = await Navigator.NavigateViewForResultAsync<SimpleDialog,object>(this, Schemes.Dialog);
			var dialogResult = await showDialog.Result;
			SimpleDialogResultText.Text = $"Dialog result: {dialogResult.SomeOrDefault()?.ToString()}";
		}
		private async void SimpleDialogCodebehindCancelClick(object sender, RoutedEventArgs args)
		{
			var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
			var showDialog = await Navigator.NavigateViewForResultAsync<SimpleDialog, object>(this, Schemes.Dialog, cancellation: cancelSource.Token);
			var dialogResult = await showDialog.Result;
			SimpleDialogResultText.Text = $"Dialog result: {dialogResult.SomeOrDefault()?.ToString()}";
		}
		public void Inject(INavigator entity) => Navigator = entity;
	}
}
