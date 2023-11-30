using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A **stateless** stream of data.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public partial interface IFeed<T> : ISignal<Message<T>>
	/* where T : record or struct */
{
}
