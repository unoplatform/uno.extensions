using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Tests.MockingApp.SingleProject;

public interface IPantryService
{
	Task<IImmutableList<string>> GetItems(CancellationToken ct);

	Task<string> GetDefaultFilter(CancellationToken ct);
}
