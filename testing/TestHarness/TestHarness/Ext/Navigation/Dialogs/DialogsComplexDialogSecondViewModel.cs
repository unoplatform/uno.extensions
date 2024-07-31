namespace TestHarness.Ext.Navigation.Dialogs;

internal class DialogsComplexDialogSecondViewModel
{
	private INavigator Navigator { get; }

	public ICommand CloseCommand { get; }

	public string? Name { get; set; }

	public DialogsComplexDialogSecondViewModel(
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
