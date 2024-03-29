﻿namespace TestHarness.Ext.Navigation.Reactive;

public partial class ReactiveFiveViewModel : BaseViewModel
{
	public ReactiveFiveViewModel(INavigator navigator, FiveModel? model) : base(navigator)
	{
		DataModel = State.Value(this, () => model);
	}

	public IState<FiveModel?> DataModel { get; }

	public async Task GoToSix()
	{
		await Navigator.NavigateViewModelAsync<ReactiveSixViewModel>(this);
	}

	public async Task GoToSixData()
	{
		// Note: NavigateDataAsync won't work since six isn't included in the routemap
		// await Navigator.NavigateDataAsync(this, data: new SixModel(new ReactiveWidget("From Five", 59)));
		await Navigator.NavigateViewModelAsync<ReactiveSixViewModel>(this, data: new SixModel(new ReactiveWidget("From Five", 59)));
	}

	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
