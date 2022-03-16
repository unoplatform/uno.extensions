//-:cnd:noEmit
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;

namespace MyExtensionsApp.Services;

public interface IDealService
{
	ValueTask<Product[]> GetDeals(CancellationToken ct);
}
