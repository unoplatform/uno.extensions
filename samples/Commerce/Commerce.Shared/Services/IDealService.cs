using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services;

public interface IDealService
{
	ValueTask<Product[]> GetDeals(CancellationToken ct);
}
