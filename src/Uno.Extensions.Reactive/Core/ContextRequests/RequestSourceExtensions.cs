using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

internal static class RequestSourceExtensions
{
	/// <summary>
	/// Send a refresh request to feed that has been subscribed using this subscriber context.
	/// WARNING Read threading consideration in remarks.
	/// </summary>
	/// <remarks>
	/// This is expected to be invoked from a background thread.
	/// Using this from the UI thread will result into an empty TokenCollection.
	/// This is due to the fact that the UI thread does not allow attached child tasks,
	/// driving the request to be re-scheduled on a background thread before flowing into the requests <see cref="IAsyncEnumerable{T}"/>.
	/// </remarks>
	/// <returns>
	/// A collection of <see cref="RefreshToken"/> which indicates the minimum version reflecting that refresh,
	/// for all source feeds that have been impacted by this request.
	/// </returns>
	public static TokenSet<RefreshToken> RequestRefresh(this IRequestSource requests)
	{
		var request = new RefreshRequest();
		requests.Send(request);

		return request.GetResult();
	}
}
