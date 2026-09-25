using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface IBasketService
{
	Task<IImmutableList<string>> GetLines(CancellationToken ct);
}
