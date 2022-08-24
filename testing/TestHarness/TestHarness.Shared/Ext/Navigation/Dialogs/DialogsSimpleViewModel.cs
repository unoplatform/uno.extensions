
namespace TestHarness.Ext.Navigation.Dialogs;

public class DialogsSimpleViewModel
{
	private INavigator Navigator { get; }

	public ICommand OkCommand { get; }

	public ICommand CloseCommand { get; }

	public string? Name { get; set; }

	public DialogsSimpleViewModel(
		INavigator navigator)
	{

		Navigator = navigator;

		OkCommand = new AsyncRelayCommand(Ok);

		CloseCommand = new AsyncRelayCommand(Close);
	}

	public async Task Ok()
	{
		await Navigator.NavigateBackWithResultAsync(this, data: new Widget { Name = Name });
	}

	public async Task Close()
	{
		await Navigator.NavigateBackAsync(this);
	}
}
