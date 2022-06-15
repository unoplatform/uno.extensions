using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

/// <summary>
/// A presentation service used by <see cref="BindableCollection"/> to request more items
/// (cf. <see cref="ISupportIncrementalLoading"/>).
/// </summary>
internal interface IPaginationService
{
	/// <summary>
	/// Event raise when any properties of the service has changed
	/// </summary>
	event EventHandler StateChanged;

	/// <summary>
	/// Indicates that the source can load more items
	/// </summary>
	bool HasMoreItems { get; }

	/// <summary>
	/// Indicates that the source is currently loading more items.
	/// </summary>
	bool IsLoadingMoreItems { get; }

	/// <summary>
	/// Request to the source to load more items.
	/// </summary>
	/// <param name="desiredCount">The desired number of items to load.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>An async operation that reflects the loading.</returns>
	Task<uint> LoadMoreItems(uint desiredCount, CancellationToken ct);
}
