
namespace TestHarness.Ext.Navigation.Dialogs;

public class DialogsSimpleViewModel
{
	private INavigator Navigator { get; }

	public ICommand AddCommand { get; }

	public string? Name { get; set; }

	public DialogsSimpleViewModel(
		INavigator navigator)
	{

		Navigator = navigator;

		AddCommand = new AsyncRelayCommand(Add);
	}

	public async Task Add()
	{
		await Navigator.NavigateBackWithResultAsync(this, data: new Widget { Name = Name });
	}
}
