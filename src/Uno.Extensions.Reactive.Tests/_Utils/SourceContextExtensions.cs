using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Tests
{
	internal static class SourceContextExtensions
	{
		/// <summary>
		/// DO NOT USE - FOR TESTS PURPOSES ONLY - Creates a child context given a request source.
		/// </summary>
		/// <param name="requests">The request source to use for the new context.</param>
		/// <param name="owner">The name of the owner of this child context.</param>
		/// <returns>A new child SourceContext.</returns>
		internal static SourceContext CreateChild(this SourceContext ctx, IRequestSource requests, [CallerMemberName] string owner = "")
			=> ctx.CreateChild(new TestContextOwner(owner), requests: requests);

		internal record TestContextOwner(string Name) : ISourceContextOwner
		{
			/// <inheritdoc />
			public IDispatcher? Dispatcher => null;
		}
	}
}
