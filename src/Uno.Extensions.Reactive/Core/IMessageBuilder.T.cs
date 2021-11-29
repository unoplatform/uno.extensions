using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of <see cref="Message{T}"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)] // Only a tag interface for extensions methods
public interface IMessageBuilder<in T> : IMessageBuilder
{
}
