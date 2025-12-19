using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// The implementation of <see cref="IBindableFactory"/> for the UWP and WinUI platform.
/// </summary>
/// <remarks>This is not intended to be used by application but instead be initialized by module initializer.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class BindableFactory : IBindableFactory
{
	/// <summary>
	/// The singleton instance.
	/// </summary>
	public static BindableFactory Instance { get; } = new BindableFactory();

	private BindableFactory()
	{
	}

	/// <inheritdoc />
	public IListFeed<T> CreateList<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
		T
	>(string name, IListState<T> source)
		=> new BindableListFeed<T>(name, source);
}
