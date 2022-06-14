using System;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A token that can be used to track a request and which can be aggregated by a <see cref="TokenSet{TToken}"/>.
/// </summary>
/// <remarks>This is deigned to response to a request sent through a <see cref="IRequestSource"/>.</remarks>
internal interface IToken
{
	/// <summary>
	/// The source signal that has issued that token.
	/// </summary>
	object Source { get; }

	/// <summary>
	/// The root ID of the subscription context for which this token has been issued.
	/// </summary>
	/// <remarks>This identifies subscription into which this token is going to be propagated.</remarks>
	uint RootContextId { get; }

	/// <summary>
	/// The sequential ID of this token.
	/// A greater ID indicates that the work associated to previous token has been completed.
	/// </summary>
	uint SequenceId { get; }
}
