using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Names of common axises
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class MessageAxises
{
	/// <summary>
	/// Name of the data axis
	/// </summary>
	public const string Data = nameof(Data);

	/// <summary>
	/// Name of the error axis.
	/// </summary>
	public const string Error = nameof(Error);

	/// <summary>
	/// Name of the progress axis.
	/// </summary>
	public const string Progress = nameof(Progress);

	internal const string BindingSource = nameof(BindingSource);
}
