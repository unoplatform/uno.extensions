using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal interface IForEachRunner : IDisposable
{
	void Prefetch();

	IDisposable? Enable();
}
