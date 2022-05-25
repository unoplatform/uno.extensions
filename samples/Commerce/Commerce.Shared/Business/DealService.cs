using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data;
using Commerce.Models;
using Uno.Extensions;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace Commerce.Services;

class DealService : IDealService
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

		return products!.Where(p => !p.Discount.IsNullOrEmpty()).ToArray();
	}
}
