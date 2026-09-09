using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Metadata describing how a feed/list-feed member of a model is fed (spec 013 — MVUX mocking).
/// Emitted by the MVUX generator on the model partial, and also hand-declarable by the author
/// (explicit declarations win/merge with the analysis). Survives as assembly metadata so the
/// external mocking generator (in a test/preview project) can read it without syntax trees.
/// </summary>
/// <remarks>
/// Classification semantics (exactly one intent per instance; multiple instances per member allowed):
/// <list type="bullet">
/// <item><description><see cref="OnParameter"/> set → the member is a <em>service-dependent input</em>
/// (fed by the named constructor parameter). These are the required inputs of a generated mock.</description></item>
/// <item><description><see cref="OnFeed"/> set → the member is <em>derived</em> from another feed member
/// (never required in a mock; recomputes over the swapped inputs).</description></item>
/// <item><description>neither set → the member is <em>independent</em>.</description></item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class FeedDependencyAttribute : Attribute
{
	/// <summary>
	/// Creates a new dependency descriptor for the given model member.
	/// </summary>
	/// <param name="member">The name of the feed/list-feed member this descriptor applies to.</param>
	public FeedDependencyAttribute(string member)
	{
		Member = member;
	}

	/// <summary>
	/// The feed/list-feed member this descriptor applies to.
	/// </summary>
	public string Member { get; }

	/// <summary>
	/// When set, the constructor parameter (service) feeding this member — marks the member as a
	/// service-dependent input.
	/// </summary>
	public string? OnParameter { get; init; }

	/// <summary>
	/// When set, another feed member this member is derived from — marks the member as derived.
	/// </summary>
	public string? OnFeed { get; init; }
}
