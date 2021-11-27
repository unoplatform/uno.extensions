using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A trigger
/// </summary>
public interface ISignal : ISignal<Unit>
{
}
