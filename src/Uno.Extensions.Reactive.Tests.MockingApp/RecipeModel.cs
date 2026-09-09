using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;


namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface IRecipeService
{
	Task<IImmutableList<int>> GetSteps(CancellationToken ct);
}

public partial class RecipeModel
{
	private readonly IRecipeService _svc;

	public RecipeModel(IRecipeService svc)
	{
		_svc = svc;
	}

	// service-dependent input (list)
	public IListFeed<int> Steps => ListFeed.Async(async ct => await _svc.GetSteps(ct));

	// independent scalar input
	public IFeed<string> Title => Feed.Async(async ct => "Recipe");

	// command → IAsyncCommand Save on the VM + __Mock_SetCommand seam (opt-in)
	public async ValueTask Save(CancellationToken ct)
	{
	}
}
