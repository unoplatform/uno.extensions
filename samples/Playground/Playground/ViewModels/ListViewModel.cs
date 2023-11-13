namespace Playground.ViewModels;

public class ListViewModel
{
	public ItemDetailsViewModel[] Items { get; } = new ItemDetailsViewModel[]
	{
		new ItemDetailsViewModel(new Widget("Fred",49.0)),
		new ItemDetailsViewModel(new Widget("Jane",34.2)),
		new ItemDetailsViewModel(new Widget("Bob",46.3)),
		new ItemDetailsViewModel(new Widget("Frank",55.2)),
		new ItemDetailsViewModel(new Widget("Matt",45.7)),
		new ItemDetailsViewModel(new Widget("Shane",99.3)),
		new ItemDetailsViewModel(new Widget("Helen",23.7)),
		new ItemDetailsViewModel(new Widget("Jo",34.5)),
		new ItemDetailsViewModel(new Widget("Mike",23.3))
	};

}
