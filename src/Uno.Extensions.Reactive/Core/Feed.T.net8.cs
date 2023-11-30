using System.Collections.Generic;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

partial class Feed<T>
{
	/// <summary>
	/// Gets or create a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerable">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> AsyncEnumerable(IAsyncEnumerable<T> enumerable)
		=> AttachedProperty.GetOrCreate(enumerable, typeof(AsyncEnumerableFeed<T>), static (en, _) => new AsyncEnumerableFeed<T>(en));
}
