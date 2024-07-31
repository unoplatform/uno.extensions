namespace TestHarness.Ext.Navigation.NavigationView;

public partial class NavigationViewDataViewModel
{
	public IState<IChefEntity> Entity { get; }

	private INavigationViewDataService Data { get; }
	private INavigator Navigator { get; }
	public NavigationViewDataViewModel(INavigator navigator, INavigationViewDataService data)
	{
		Data = data;
		Navigator = navigator;

		Entity = State<IChefEntity>.Empty(this);
		Entity.ForEachAsync(async (entity, ct) =>
		{
			if (entity is not null)
			{
				await Navigator.NavigateDataAsync(this, entity, qualifier: Qualifiers.Nested);
				_ = Entity.Update(_ => default(IChefEntity), CancellationToken.None);
			}
		});
	}
}

