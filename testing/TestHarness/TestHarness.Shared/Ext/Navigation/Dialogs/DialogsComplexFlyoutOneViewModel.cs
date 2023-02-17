namespace TestHarness.Ext.Navigation.Dialogs;

public class DialogsComplexFlyoutOneViewModel
{
	private INavigator Navigator { get; }

	public ICommand CloseCommand { get; }

	public string? Name { get; set; }

	public DialogsComplexFlyoutOneViewModel(
		INavigator navigator)
	{

		Navigator = navigator;

		CloseCommand = new AsyncRelayCommand(Close);
	}

	public async Task Close()
	{
		await Navigator.NavigateBackAsync(this, qualifier: Qualifiers.Root);
	}
}
