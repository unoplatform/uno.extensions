#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Uno.AuthenticationBroker;
using Uno.Foundation.Extensibility;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// The desktop broker with a fake browser: <see cref="OnLaunch"/> stands in for the system
/// browser, and every log line goes to <see cref="Logs"/>.
/// </summary>
internal sealed class TestBroker : DesktopWebAuthenticationBrokerProvider
{
	private static readonly Func<Uri, CancellationToken, Task> NeverNavigates = (_, _) => Task.CompletedTask;

	public TestBroker()
		: this(new CapturingLogger())
	{
	}

	private TestBroker(CapturingLogger logs)
		: base(logs)
	{
		Logs = logs;
	}

	/// <summary>The fake browser; defaults to never navigating anywhere.</summary>
	public Func<Uri, CancellationToken, Task> OnLaunch { get; set; } = NeverNavigates;

	public Uri? LaunchedRequestUri { get; private set; }

	public CapturingLogger Logs { get; }

	/// <summary>Back to the never-navigating default, for the shared instance between tests.</summary>
	public void Reset()
	{
		OnLaunch = NeverNavigates;
		LaunchedRequestUri = null;
	}

	protected override Task LaunchBrowserAsync(Uri requestUri, CancellationToken ct)
	{
		LaunchedRequestUri = requestUri;
		return OnLaunch(requestUri, ct);
	}
}

/// <summary>
/// The one <see cref="IWebAuthenticationBrokerProvider"/> Uno's static
/// <see cref="Windows.Security.Authentication.Web.WebAuthenticationBroker"/> binds to for the
/// life of the process: it resolves its provider once, in its static constructor, so every test
/// that drives the static broker (or anything built on it) shares this instance - registered
/// before anything in the process touches that type.
/// </summary>
internal static class StaticBrokerRegistration
{
	private static int _registered;

	public static TestBroker Shared { get; } = new();

	/// <summary>
	/// Registers <see cref="Shared"/> (once) and proves it is what the registry hands out. Fails
	/// loudly otherwise: a stray earlier registration would pin the real broker, and driving a
	/// flow through it would open a real browser on the test agent.
	/// </summary>
	public static TestBroker EnsureRegistered()
	{
		if (Interlocked.Exchange(ref _registered, 1) == 0)
		{
			ApiExtensibility.Register(typeof(IWebAuthenticationBrokerProvider), _ => Shared);
		}

		ApiExtensibility.CreateInstance<IWebAuthenticationBrokerProvider>(null!, out var registered)
			.Should().BeTrue("the test double must be the registered broker provider");
		ReferenceEquals(registered, Shared)
			.Should().BeTrue("another IWebAuthenticationBrokerProvider was registered before this suite; the static broker would open a real browser");

		Shared.Reset();
		return Shared;
	}
}
#endif
