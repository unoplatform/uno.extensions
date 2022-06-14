using System;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A request for a source ListFeed to load more items
/// </summary>
/// <param name="DesiredPageSize">The desired number of items.</param>
internal record PageRequest(uint DesiredPageSize) : IContextRequest, IContextRequest<PageToken>
{
	private ImmutableList<PageToken> _result = ImmutableList<PageToken>.Empty;

	/// <inheritdoc />
	public void Register(PageToken token)
		=> ImmutableInterlocked.Update(ref _result, (list, t) => list.Add(t), token);

	/// <summary>
	/// Get a set containing all token that has been issued in response of this request.
	/// </summary>
	public TokenSet<PageToken> GetResult()
		=> new(_result);
}
