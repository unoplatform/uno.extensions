using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Names of common axes
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class MessageAxes
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

	/// <summary>
	/// Name of the refresh axis.
	/// </summary>
	internal const string Refresh = nameof(Refresh);

	/// <summary>
	/// Name of the axe used to de-bounce data bound values
	/// </summary>
	internal const string BindingSource = nameof(BindingSource);
}
