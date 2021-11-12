using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

internal interface IForEachRunner : IDisposable
{
	void Prefetch();

	IDisposable? Enable();
}
