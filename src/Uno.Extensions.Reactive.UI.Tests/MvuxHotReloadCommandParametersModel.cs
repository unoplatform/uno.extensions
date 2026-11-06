namespace Uno.Extensions.Reactive.WinUI.Tests;

// Hot-reload fixture for #3295: the command starts with only a CancellationToken and is hot reloaded into a command
// whose parameters are bound to the states of the same names. Keep the command on a single line: the test replaces it.
public partial record MvuxHotReloadCommandParametersModel(string Prefix)
{
	public IListState<string> Items => ListState<string>.Empty(this);

	public IState<string> NewTitle => State.Value(this, () => "title");

	public IState<int> NewCount => State.Value(this, () => 1);

	public IState<MvuxHotReloadCommandParametersCategory> NewCategory => State.Value(this, () => MvuxHotReloadCommandParametersCategory.Food);

	public IState<DateOnly> NewDate => State.Value(this, () => new DateOnly(2026, 1, 2));

	public async ValueTask Add(CancellationToken ct) { var newTitle = await NewTitle; var newCount = await NewCount; var newCategory = await NewCategory; var newDate = await NewDate; await Items.AddAsync($"{Prefix}-{newTitle}-{newCount}-{newCategory}-{newDate:yyyy-MM-dd}-original", ct); }
}

public enum MvuxHotReloadCommandParametersCategory
{
	None,
	Food,
}
