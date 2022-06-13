using System;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Testing;

public record RefreshConstraint
{
	public object? Source { get; init; }

	public uint? RootContextId { get; init; }

	public uint? Version { get; init; }

	internal int Match(RefreshToken version)
	{
		var score = 0;

		if (Source is not null)
		{
			score += Source == version.Source ? 1 : -100;
		}

		if (RootContextId is not null)
		{
			score += RootContextId == version.RootContextId ? 1 : -100;
		}

		if (Version is not null)
		{
			score += Version == version.SequenceId ? 1 : -100;
		}

		return score;
	}
}
