
namespace TestHarness.Ext.Navigation.Dialogs;

public partial class DialogsFlyoutsViewModel
{
	private INavigator Navigator { get; }

	public DialogsFlyoutsViewModel(
		INavigator navigator)
	{

		//BindableDialogsFlyoutsViewModel
		Navigator = navigator;

		FlyoutData = State< DialogsFlyoutsData>.Empty(this);
		FlyoutData.ForEachAsync(async (obj, ct) =>
		{
			if (obj is not null &&
			obj.GetType() != typeof(object))
			{
				await Navigator.NavigateDataAsync(this, obj);
			}
		});
	}

	public IState<DialogsFlyoutsData> FlyoutData { get; }


}


public class DialogsFlyoutsData
{
	public Guid Id { get; } = Guid.NewGuid();
}
