using System;
using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page used to verify <see cref="UI.NavigationRouteUpdateHandler"/>'s
/// pending-request retry. Its constructor throws while
/// <see cref="HotReloadPendingRetryGate.IsAvailable"/> returns <c>false</c>,
/// so the first navigation attempt routes through <c>FrameNavigator.Show</c>
/// which catches the exception and returns null. ControlNavigator records the
/// failed request as pending. The test flips the gate via C# hot-reload —
/// after the resolver is rebuilt and the retry walk runs, this page must
/// successfully construct on the second attempt and land in the visual tree.
/// </summary>
public sealed partial class HotReloadPendingRetryPage : Page
{
	public HotReloadPendingRetryPage()
	{
		if (!HotReloadPendingRetryGate.IsAvailable())
		{
			throw new InvalidOperationException(
				"HotReloadPendingRetryPage cannot be constructed while the gate is closed. " +
				"This simulates a hot-reload-pending dependency that is not yet present in the assembly.");
		}

		DisplayedValue = "pending-retry-loaded";
		Content = new TextBlock { Text = DisplayedValue };
	}

	public string DisplayedValue { get; } = string.Empty;
}
