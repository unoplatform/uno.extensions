using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data.Models;

namespace Commerce.Data;

public interface ICartEndpoint
{
	ValueTask<CartData> Get(CancellationToken ct);
}
