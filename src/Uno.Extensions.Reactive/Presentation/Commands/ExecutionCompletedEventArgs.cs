using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// Args used by <see cref="AsyncCommand.ExecutionCompleted"/> event.
/// </summary>
/// <param name="Id">A unique identifier of the execution that can be used to track completion</param>
/// <param name="Parameter">The parameter provided by the view.</param>
/// <param name="Error">The exception object if an error has been thrown while executing, null if execution succeed</param>
public record ExecutionCompletedEventArgs(Guid Id, object? Parameter, Exception? Error);
