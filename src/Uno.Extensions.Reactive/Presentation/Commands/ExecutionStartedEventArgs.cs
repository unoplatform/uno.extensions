using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// Args used by <see cref="AsyncCommand.ExecutionStarted"/> event.
/// </summary>
/// <param name="Id">A unique identifier of the execution that can be used to track completion</param>
/// <param name="Parameter">The parameter provided by the view.</param>
public record ExecutionStartedEventArgs(Guid Id, object? Parameter);
