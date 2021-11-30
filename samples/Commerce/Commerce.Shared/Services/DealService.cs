using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Uno.Extensions;
using Uno.Extensions.Serialization;

namespace Commerce.Services;

class DealService : IDealService
{
    private readonly IJsonDataService<Product> _productDataService;

    public DealService(IJsonDataService<Product> products)
    {
        _productDataService = products;
        _productDataService.DataFile = ProductService.ProductDataFile;
    }

    public async ValueTask<Product[]> GetDeals(CancellationToken ct)
    {
        var products = await _productDataService.GetEntities();

        return products.Where(p => !p.Discount.IsNullOrEmpty()).ToArray();
    }
}
