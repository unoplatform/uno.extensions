using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// Initialize this module to register services provided by this UI module for the reactive framework.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ModuleInitializer
{
	private static int _isInitialized;

	/// <summary>
	/// Register the <seealso cref="DispatcherQueueProvider"/> as provider of <see cref="IDispatcher"/> for the reactive platform.
	/// </summary>
	/// <remarks>This method is flagged with ModuleInitializer attribute and should not be used by application.</remarks>

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
	[ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
	public static void Initialize()
	{
		// This method might be invoked more than once (ModuleInitializer from this assembly, the generated initializer of assemblies that depends on this assembly, ... and user application code.
		if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) is 0)
		{
			DispatcherHelper.GetForCurrentThread = DispatcherQueueProvider.GetForCurrentThread;
			BindableHelper.ConfigureFactory(BindableFactory.Instance);
		}
	}
}
