using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Reactive.Bindings.Collections;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Utils.Debugging;

namespace Uno.Extensions.Reactive.UI.Utils;

/// <summary>
/// Set of helpers to deal with <see cref="SourceContext"/> from the UI
/// </summary>
internal class SourceContextHelper
{
	public static SourceContext CreateChildContext(SourceContext context, FrameworkElement element, IRequestSource requests)
		=> context.CreateChild(new UIElementContextOwner(element), requests);

	private class UIElementContextOwner : ISourceContextOwner
	{
		public UIElementContextOwner(FrameworkElement element)
		{
			Name = DebugConfiguration.IsDebugging
				? (element.Name ?? element.GetType().Name) + "-" + element.GetHashCode().ToString("X8")
				: "-debugging disabled-";
			Dispatcher = DispatcherQueueProvider.GetForCurrentThread()
				?? throw new InvalidOperationException("This must be created from the UI thread of the given element.");
		}

		public string Name { get; }

		public IDispatcher Dispatcher { get; }
	}

	internal static SourceContext CreateChildContext<T>(SourceContext context, BindableCollection bindableCollection, IListState<T> state, IRequestSource requests)
		=> context.CreateChild(new NamedOwner("BindableCollection for " + state.Context.Owner.Name), requests);

	private record NamedOwner(string Name) : ISourceContextOwner
	{
		public IDispatcher? Dispatcher => null;
	}
}
