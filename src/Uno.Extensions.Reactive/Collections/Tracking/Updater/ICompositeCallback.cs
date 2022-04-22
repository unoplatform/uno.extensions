using System;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// A composite callback which contains callbacks for multiple phases and potentially some other changes
/// </summary>
internal interface ICompositeCallback
{
	/// <summary>
	/// Invokes all the callbacks at once
	/// </summary>
	void Invoke(CallbackPhase phases, bool silently);
}
