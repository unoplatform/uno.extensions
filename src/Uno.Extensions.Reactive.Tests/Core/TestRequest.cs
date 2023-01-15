using System;
using System.Linq;
using System.Threading;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Tests.Core;

[ImplicitKeys(IsEnabled = false)]
internal record TestRequest(int? _id = null) : IContextRequest
{
	private static int _nextId;

	public int Id { get; } = _id ?? Interlocked.Increment(ref _nextId);
}
