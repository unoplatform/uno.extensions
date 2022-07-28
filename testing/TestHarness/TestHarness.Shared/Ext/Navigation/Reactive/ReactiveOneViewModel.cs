using Uno.Extensions.Navigation;

namespace TestHarness.Ext.Navigation.Reactive;

public partial record ReactiveOneViewModel (INavigator Navigator)
{
	public async Task GoToTwo()
	{
		await Navigator.NavigateViewModelAsync<ReactiveTwoViewModel>(this);
	}

	public async Task GoToTwoData()
	{
		await Navigator.NavigateDataAsync(this, data:new TwoModel(new ReactiveWidget("From Two",56)));
	}
	public async Task ShowDialog()
	{
		var result = await Navigator.ShowMessageDialogAsync<object>(this, "LocalizedConfirm");
	}
}

public record OneModel(ReactiveWidget Widget);
public record TwoModel(ReactiveWidget Widget);
public record ThreeModel(ReactiveWidget Widget);
public record FourModel(ReactiveWidget Widget);
public record FiveModel(ReactiveWidget Widget);

public record ReactiveWidget(string Name, double Weight);
