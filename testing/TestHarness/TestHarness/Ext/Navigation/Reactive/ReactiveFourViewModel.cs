namespace TestHarness.Ext.Navigation.Reactive;

public partial class ReactiveFourViewModel : BaseViewModel
{
	public ReactiveFourViewModel(INavigator navigator, FourModel? model) : base(navigator)
	{
		DataModel = State.Value(this, () => model);
	}

	public IState<FourModel?> DataModel { get; }

	public async Task GoToFive()
	{
		await Navigator.NavigateViewModelAsync<ReactiveFiveViewModel>(this);
	}

	public async Task GoToFiveData()
	{
		await Navigator.NavigateDataAsync(this, data: new FiveModel(new ReactiveWidget("From Four",24)));
	}


	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
