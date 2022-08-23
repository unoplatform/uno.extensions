namespace TestHarness.Ext.Navigation.Reactive;

public partial class ReactiveThreeViewModel : BaseViewModel
{
	public ReactiveThreeViewModel(INavigator navigator, ThreeModel? model) : base(navigator)
	{
		DataModel = State.Value(this, () => model);
	}

	public IState<ThreeModel?> DataModel { get; }

	public async Task GoToFour()
	{
		await Navigator.NavigateViewModelAsync<ReactiveFourViewModel>(this);
	}

	public async Task GoToFourData()
	{
		await Navigator.NavigateDataAsync(this, data: new FourModel(new ReactiveWidget("From Three",67)));
	}


	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
