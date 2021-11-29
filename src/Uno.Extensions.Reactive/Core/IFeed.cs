using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A **stateless** stream of data.
/// </summary>
/// <typeparam name="T">The type of the data</typeparam>
public interface IFeed<T> : ISignal<Message<T>>
	/* where T : record */
{
}
