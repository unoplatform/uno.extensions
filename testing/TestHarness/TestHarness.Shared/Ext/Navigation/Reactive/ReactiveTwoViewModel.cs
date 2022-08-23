namespace TestHarness.Ext.Navigation.Reactive;

public partial class ReactiveTwoViewModel: BaseViewModel
{
	public ReactiveTwoViewModel(INavigator navigator, TwoModel? model):base(navigator)
	{
		DataModel = State.Value(this, () => model);
		NextModel = State.Value(this, () => new ThreeModel(new ReactiveWidget(model?.Widget?.Name??"Next from Two", model?.Widget?.Weight??34)));
	}

	public IState<TwoModel?> DataModel { get; }
	public IState<ThreeModel> NextModel { get; }

	public async Task GoToThree()
	{
		await Navigator.NavigateViewModelAsync<ReactiveThreeViewModel>(this);
	}

	public async Task GoToThreeData()
	{
		await Navigator.NavigateDataAsync(this, data: new ThreeModel(new ReactiveWidget("From Two",34)));
	}


	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}


public class BaseViewModel
{
	protected INavigator Navigator { get; }
	protected BaseViewModel(INavigator navigator)
	{
		Navigator = navigator;
	}
}
