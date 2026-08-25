using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;

namespace Uno.Extensions.Authentication.MSAL.UI.Tests;

/// <summary>
/// Stands in for the browser during interactive sign-in: returns the redirect URI with an
/// authorization code instead of opening a web view.
/// </summary>
/// <remarks>
/// <para>
/// This is the hook MSAL's own UI tests use (their <c>SeleniumWebUI</c>), and it is what makes an
/// unattended interactive login possible. Microsoft flags <see cref="ICustomWebUi"/> as
/// discouraged for production apps - that guidance is about shipping apps hosting their own
/// browser, not about test automation, which is its documented purpose.
/// </para>
/// <para>
/// The real browser is the only part of the flow being replaced. PKCE, code redemption, cache
/// writes and account resolution all still run through MSAL for real.
/// </para>
/// </remarks>
internal sealed class StubWebUi : ICustomWebUi
{
	private readonly StubEntra _tenant;
	private readonly TimeSpan _delay;

	/// <param name="tenant">Builds the response URI, echoing the request's <c>state</c>.</param>
	/// <param name="delay">
	/// Simulated user think-time. Used by the cancellation test to get a deterministic window in
	/// which to cancel, instead of racing the flow.
	/// </param>
	public StubWebUi(StubEntra tenant, TimeSpan? delay = null)
	{
		_tenant = tenant;
		_delay = delay ?? TimeSpan.Zero;
	}

	/// <summary>Set when the sign-in "UI" was invoked, so tests can assert silent paths did not prompt.</summary>
	public bool WasInvoked { get; private set; }

	public async Task<Uri> AcquireAuthorizationCodeAsync(
		Uri authorizationUri,
		Uri redirectUri,
		CancellationToken cancellationToken)
	{
		WasInvoked = true;

		if (_delay > TimeSpan.Zero)
		{
			// Honours the token so a cancelled login surfaces as OperationCanceledException from
			// here, which is exactly where a real browser-based flow would observe it.
			await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
		}

		cancellationToken.ThrowIfCancellationRequested();

		return _tenant.BuildAuthorizationResponse(authorizationUri, redirectUri);
	}
}
