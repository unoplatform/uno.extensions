using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Extensions.Authentication;
using Uno.Extensions.Authentication.Custom;
using Uno.Extensions.Storage.KeyValueStorage;

namespace Uno.Extensions.Authentication.Tests;

/// <summary>
/// Coverage of the Custom provider through the hosting API - login, refresh, logout and
/// cancellation - in a bare (non-Uno) host with in-memory storage. Spec 013, item 13.
/// </summary>
[TestClass]
public class Given_CustomAuthentication
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	private static CancellationTokenSource Cts() => new(Timeout);

	private static IHost BuildHost(Action<ICustomAuthenticationBuilder> configure) =>
		Host.CreateDefaultBuilder()
			.ConfigureServices(services => services
				// UseAuthentication resolves the token cache's storage via the default-instance
				// registration that UnoHost normally provides; a bare host supplies it directly.
				.SetDefaultInstance<IKeyValueStorage, FakeKeyValueStorage>())
			.UseAuthentication(auth => auth.AddCustom(configure))
			.Build();

	[TestMethod]
	public async Task When_Login_Then_TokensCached()
	{
		using var host = BuildHost(custom => custom
			.Login((sp, dispatcher, cache, credentials, ct) =>
				ValueTask.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>
				{
					[TokenCacheExtensions.AccessTokenKey] = "custom-access-token",
				})));
		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		var tokens = host.Services.GetRequiredService<ITokenCache>();
		using var cts = Cts();

		var result = await authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeTrue();
		(await tokens.AccessTokenAsync(cts.Token)).Should().Be("custom-access-token");
	}

	[TestMethod]
	public async Task When_LoginCallbackReturnsNull_Then_NotAuthenticated()
	{
		using var host = BuildHost(custom => custom
			.Login((sp, dispatcher, cache, credentials, ct) =>
				ValueTask.FromResult<IDictionary<string, string>?>(default)));
		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		using var cts = Cts();

		var result = await authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeFalse();
		(await authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_Refresh_Then_TokensRenewed()
	{
		using var host = BuildHost(custom => custom
			.Login((sp, dispatcher, cache, credentials, ct) =>
				ValueTask.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>
				{
					[TokenCacheExtensions.AccessTokenKey] = "custom-access-token",
				}))
			.Refresh((sp, cache, tokens, ct) =>
				ValueTask.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>
				{
					[TokenCacheExtensions.AccessTokenKey] = "custom-refreshed-token",
				})));
		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		var tokens = host.Services.GetRequiredService<ITokenCache>();
		using var cts = Cts();

		await authentication.LoginAsync(default, cancellationToken: cts.Token);
		var refreshed = await authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeTrue();
		(await tokens.AccessTokenAsync(cts.Token)).Should().Be("custom-refreshed-token");
	}

	[TestMethod]
	public async Task When_RefreshFails_Then_NotAuthenticated()
	{
		using var host = BuildHost(custom => custom
			.Login((sp, dispatcher, cache, credentials, ct) =>
				ValueTask.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>
				{
					[TokenCacheExtensions.AccessTokenKey] = "custom-access-token",
				}))
			.Refresh((sp, cache, tokens, ct) =>
				ValueTask.FromResult<IDictionary<string, string>?>(default)));
		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		using var cts = Cts();

		await authentication.LoginAsync(default, cancellationToken: cts.Token);
		var refreshed = await authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeFalse("a refresh callback returning null must not report the user as authenticated");
		(await authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_LogoutCallbackRefuses_Then_StillAuthenticated()
	{
		using var host = BuildHost(custom => custom
			.Login((sp, dispatcher, cache, credentials, ct) =>
				ValueTask.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>
				{
					[TokenCacheExtensions.AccessTokenKey] = "custom-access-token",
				}))
			.Logout((sp, dispatcher, cache, tokens, ct) => ValueTask.FromResult(false)));
		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		using var cts = Cts();

		await authentication.LoginAsync(default, cancellationToken: cts.Token);
		var loggedOut = await authentication.LogoutAsync(default, cts.Token);

		loggedOut.Should().BeFalse();
		(await authentication.IsAuthenticated(cts.Token)).Should().BeTrue(
			"a logout the provider refused must keep the session");
	}

	[TestMethod]
	public async Task When_LoginCancelled_Then_CancellationPropagates()
	{
		using var host = BuildHost(custom => custom
			.Login(async (sp, dispatcher, cache, credentials, ct) =>
			{
				await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct);
				return default(IDictionary<string, string>?);
			}));
		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

		Func<Task> act = () => authentication.LoginAsync(default, cancellationToken: cts.Token).AsTask();

		await act.Should().ThrowAsync<OperationCanceledException>(
			"the caller's token must reach the provider callback");
	}
}
