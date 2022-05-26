

namespace Commerce.Business;

public class DealService : IDealService
{
	private readonly IStorage _dataService;
	private readonly ISerializer _serializer;

	public DealService(IStorage dataService, ISerializer serializer)
	{
		_dataService = dataService;
		_serializer = serializer;
	}


	public async ValueTask<Product[]> GetAll(CancellationToken ct)
	{
		var products = await _dataService.ReadFileAsync<Product[]>(_serializer, ProductsEndpoint.ProductDataFile);

		return products!.Where(p => !string.IsNullOrWhiteSpace(p.Discount)).ToArray();
	}
}
