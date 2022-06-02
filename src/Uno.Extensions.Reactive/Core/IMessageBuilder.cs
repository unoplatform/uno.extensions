using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of <see cref="Message{T}"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)] // Only a tag interface for extensions methods
public interface IMessageBuilder
{
	/// <summary>
	/// Gets the raw value of a message axis.
	/// </summary>
	/// <param name="axis">The axis to set.</param>
	/// <returns>The raw value of the axis among the changes made to that value compared to the previous value.</returns>
	/// <remarks>
	/// This gives access to raw <see cref="MessageAxisValue"/> for extensibility but it should not be used directly.
	/// Prefer to use dedicated extensions methods to access to values.
	/// </remarks>
	/// <remarks>
	/// Getting the value of an axis that is not set will not throw,
	/// it will instead return <see cref="MessageAxisValue.Unset"/>.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)] // Use dedicated extensions methods
	(MessageAxisValue value, IChangeSet? changes) Get(MessageAxis axis);

	/// <summary>
	/// Sets the raw value of a message axis.
	/// </summary>
	/// <param name="axis">The axis to set.</param>
	/// <param name="value">The raw value of the axis.</param>
	/// <param name="changes">The changes made compared to the previous value of the axis.</param>
	/// <remarks>
	/// This gives access to raw <see cref="MessageAxisValue"/> for extensibility but it should not be used directly.
	/// Prefer to use dedicated extensions methods to access to values.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)] // Use dedicated extensions methods
	void Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes = null);
}
