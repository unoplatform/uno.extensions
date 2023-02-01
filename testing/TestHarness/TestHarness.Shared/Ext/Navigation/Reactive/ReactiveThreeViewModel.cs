namespace TestHarness.Ext.Navigation.Reactive;

public partial class ReactiveThreeViewModel : BaseViewModel
{
	public ReactiveThreeViewModel(INavigator navigator, IFeed<ThreeModel>? model) : base(navigator)
	{
		DataModel = model ?? Feed.Async( async ct => new ThreeModel(new ReactiveWidget("Empty",0.0)));
	}

	public IFeed<ThreeModel> DataModel { get; }

	public async Task GoToFour()
	{
		await Navigator.NavigateViewModelAsync<ReactiveFourViewModel>(this);
	}

	public async Task GoToFourData()
	{
		await Navigator.NavigateDataAsync(this, data: new FourModel(new ReactiveWidget("From Three", 67)));
	}


	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
