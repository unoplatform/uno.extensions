using System;
using System.Linq;
using Uno.Extensions.Collections;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection;

internal class CurrentSourceUpdateEventArgs
{
	public CurrentSourceUpdateEventArgs(IObservableCollection from, IObservableCollection to)
	{
		From = from;
		To = to;
	}

	public IObservableCollection From { get; }

	public IObservableCollection To { get; }
}
