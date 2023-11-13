using Uno.Extensions.Navigation;
using Uno.Extensions.Reactive.Bindings;

namespace TestHarness.Ext.Navigation.Reactive;

public partial class ReactiveSixViewModel : BaseViewModel
{
	public ReactiveSixViewModel(INavigator navigator, SixModel? model) : base(navigator)
	{
		DataModel = State.Value(this, () => model);
	}

	public IState<SixModel?> DataModel { get; }

	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
