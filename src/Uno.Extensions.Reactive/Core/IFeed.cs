using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface IFeed<T> : ISignal<Message<T>>
	/* where T : record */
{
}
