using System;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The <see cref="MessageAxis"/> for the refresh information.
/// </summary>
internal class RefreshAxis : MessageAxis<TokenSet<RefreshToken>>
{
	public static RefreshAxis Instance { get; } = new();

	private RefreshAxis()
		: base(MessageAxes.Refresh, TokenSet<RefreshToken>.Aggregate)
	{
	}

	/// <inheritdoc />
	public override MessageAxisValue ToMessageValue(TokenSet<RefreshToken>? value)
		// For the refresh we ignore the RefreshToken.Initial which is used only to capture the Source and the subscription context
		=> value?.Tokens is {Count:1} singleToken && singleToken[0].SequenceId == 0
			? MessageAxisValue.Unset
			: base.ToMessageValue(value);
}
