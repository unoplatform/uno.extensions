using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive;

public sealed class Signal : ISignal, IDisposable//, ICommand
{
	private readonly AsyncEnumerableSubject<Unit> _subject = new(replay: false);

	public void Raise()
		=> _subject.SetNext(Unit.Default);

	///// <inheritdoc />
	//public event EventHandler CanExecuteChanged;

	///// <inheritdoc />
	//bool ICommand.CanExecute(object parameter)
	//	=> true;

	///// <inheritdoc />
	//void ICommand.Execute(object parameter)
	//	=> _subject.SetNext(default);

	/// <inheritdoc />
	IAsyncEnumerable<Unit> ISignal<Unit>.GetSource(SourceContext context, CancellationToken ct)
		=> _subject;

	/// <inheritdoc />
	public void Dispose()
		=> _subject.TryComplete();
}
