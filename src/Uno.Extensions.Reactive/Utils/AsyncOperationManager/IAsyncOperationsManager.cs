using System;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal interface IAsyncOperationsManager : IDisposable, IObserver<AsyncAction>
{
	Task Task { get; }
}
