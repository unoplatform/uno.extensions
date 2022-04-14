using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Uno.Extensions;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace Commerce.Services;

class DealService : IDealService
{
	private readonly IStorage _dataService;
	private readonly IStreamSerializer _streamSerializer;

	public DealService(IStorage dataService, IStreamSerializer streamSerializer)
	{
		_dataService = dataService;
		_streamSerializer = streamSerializer;
	}


    public async ValueTask<Product[]> GetDeals(CancellationToken ct)
    {
		var products = await _dataService.ReadFileAsync<Product[]>(_streamSerializer, ProductService.ProductDataFile);

        return products!.Where(p => !p.Discount.IsNullOrEmpty()).ToArray();
    }
}
