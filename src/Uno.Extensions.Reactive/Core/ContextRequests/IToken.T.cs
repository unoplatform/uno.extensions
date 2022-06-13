using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A <see cref="IToken"/> which can managed by the common <see cref="CoercingRequestManager{TRequest,TToken}"/> or <see cref="SequentialRequestManager{TRequest,TToken}"/>.
/// </summary>
/// <typeparam name="TToken">The type of the token.</typeparam>
internal interface IToken<out TToken> : IToken
{
	/// <summary>
	/// Issue the next token.
	/// </summary>
	/// <returns>The next token.</returns>
	TToken Next();
}
