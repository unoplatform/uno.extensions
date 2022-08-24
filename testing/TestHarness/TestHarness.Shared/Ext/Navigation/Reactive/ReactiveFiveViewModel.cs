namespace TestHarness.Ext.Navigation.Reactive;

public partial class ReactiveFiveViewModel : BaseViewModel
{
	public ReactiveFiveViewModel(INavigator navigator, FiveModel? model) : base(navigator)
	{
		DataModel = State.Value(this, () => model);
	}

	public IState<FiveModel?> DataModel { get; }

	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
