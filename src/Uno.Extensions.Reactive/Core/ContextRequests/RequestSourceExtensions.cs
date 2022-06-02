using System;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

internal static class RequestSourceExtensions
{
	/// <summary>
	/// Send a refresh request to feed that has been subscribed using this subscriber context.
	/// </summary>
	/// <returns>
	/// A collection of <see cref="RefreshToken"/> which indicates the minimum version reflecting that refresh,
	/// for all source feeds that have been impacted by this request.
	/// </returns>
	public static RefreshTokenCollection RequestRefresh(this IRequestSource requests)
	{
		var request = new RefreshRequest();
		requests.Send(request);

		return request.GetResult();
	}
}
