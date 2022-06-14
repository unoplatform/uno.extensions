using System;
using System.Linq;
using Windows.Foundation.Collections;

namespace Uno.Extensions.Reactive.Bindings.Collections;

/// <summary>
/// Null pattern implementation of <see cref="ICollectionViewGroup"/>.
/// </summary>
internal class NullCollectionViewGroup : ICollectionViewGroup
{
	/// <inhertidoc />
	public object? Group { get; }

	/// <inhertidoc />
	public IObservableVector<object> GroupItems { get; } = new NullObservableVector<object>();
}
