using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive.Messaging;

namespace Commerce.ViewModels;

public partial class DealsViewModel
{
	private readonly IDealService _dealService;
	private readonly IProductService _productService;

	private readonly Signal _refreshFavorites = new();

	public DealsViewModel(IDealService dealService, IProductService productService, IMessenger messenger)
	{
		_dealService = dealService;
		_productService = productService;

		messenger.Observe(Favorites, p => p.ProductId);
	}

	public IListFeed<Product> Items => ListFeed<Product>.AsyncPaginated(async (page, ct) =>
	{
		var results = await _dealService.GetPaginated(ct, (int)page.CurrentCount, (int)(page.DesiredSize ?? 20));

		return results;
	});

	public IListState<Product> Favorites => ListState.Async(this, _productService.GetFavorites);
}
