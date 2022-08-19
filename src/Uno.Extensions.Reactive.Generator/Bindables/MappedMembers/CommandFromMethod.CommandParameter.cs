using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal partial record CommandFromMethod
{
	private record CommandParameter(IParameterSymbol Symbol, IPropertySymbol? FeedProperty = null, bool IsCancellation = false)
	{
		public bool IsCommandParameter => FeedProperty is null && !IsCancellation;

		public bool IsFeedParameter => FeedProperty is not null;
	}
}
