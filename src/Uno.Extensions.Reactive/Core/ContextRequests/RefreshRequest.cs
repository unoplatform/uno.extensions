using System;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

internal sealed record RefreshRequest : IContextRequest
{
	private ImmutableList<RefreshToken> _result = ImmutableList<RefreshToken>.Empty;

	public void Register(RefreshToken refreshToken)
		=> ImmutableInterlocked.Update(ref _result, (list, version) => list.Add(version), refreshToken);

	public RefreshTokenCollection GetResult()
		=> new(_result);
}
