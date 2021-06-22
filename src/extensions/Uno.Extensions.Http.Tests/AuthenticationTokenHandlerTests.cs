using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Http.Handlers;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class AuthenticationTokenHandlerTests
	{
		private const string DefaultRequestUri = "http://wwww.test.com";

		[Fact]
		public async Task It_AddsAccessToken_If_Token()
		{
			var authenticationToken = new TestToken("AccessToken1");
			var authorizationHeader = default(AuthenticationHeaderValue);

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken unauthorizedToken)
				=> Task.CompletedTask;

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) =>
				{
					authorizationHeader = r.Headers.Authorization;

					return Task.FromResult(new HttpResponseMessage());
				}));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			authorizationHeader.Parameter.Should().Be(authenticationToken.AccessToken);
		}

		[Fact]
		public async Task It_RemovesAuthorizationHeader_If_NoToken()
		{
			var authorizationHeader = default(AuthenticationHeaderValue);

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(default(TestToken));

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken unauthorizedToken)
				=> Task.CompletedTask;

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) =>
				{
					authorizationHeader = r.Headers.Authorization;

					return Task.FromResult(new HttpResponseMessage());
				}));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			authorizationHeader.Should().BeNull();
		}

		[Fact]
		public async Task It_Doesnt_GetToken_If_NoAuthorization()
		{
			var isTokenRequested = false;
			var authenticationToken = new TestToken("AccessToken1");

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
			{
				isTokenRequested = true;

				return Task.FromResult(authenticationToken);
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken unauthorizedToken)
				=> Task.CompletedTask;

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage())));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			await httpClient.GetAsync(DefaultRequestUri);

			isTokenRequested.Should().BeFalse();
		}

		[Fact]
		public async Task It_Doesnt_RefreshToken_If_Authorized()
		{
			var refreshedToken = false;
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");
			var refreshedAuthenticationToken = new TestToken("AccessToken2", "RefreshToken2");

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken unauthorizedToken)
				=> Task.CompletedTask;

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				refreshedToken = true;

				return Task.FromResult(refreshedAuthenticationToken);
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage())));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			refreshedToken.Should().BeFalse();
		}

		[Fact]
		public async Task It_Retries_With_RefreshedToken()
		{
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");
			var refreshedAuthenticationToken = new TestToken("AccessToken2", "RefreshToken2");
			var authorizationHeaders = new List<AuthenticationHeaderValue>();

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken unauthorizedToken)
				=> Task.CompletedTask;

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
				=> Task.FromResult(refreshedAuthenticationToken);

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) =>
				{
					authorizationHeaders.Add(r.Headers.Authorization);

					var isUnauthorized = r.Headers.Authorization.Parameter == authenticationToken.AccessToken;

					return Task.FromResult(new HttpResponseMessage(isUnauthorized ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));
				}));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			authorizationHeaders.Count.Should().Be(2);
			authorizationHeaders.First().Parameter.Should().Be(authenticationToken.AccessToken);
			authorizationHeaders.ElementAt(1).Parameter.Should().Be(refreshedAuthenticationToken.AccessToken);
		}

		[Fact]
		public async Task It_NotifiesSessionExpired_If_Unauthorized_And_CantRefresh()
		{
			var sessionExpired = false;
			var authenticationToken = new TestToken("AccessToken1");

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			sessionExpired.Should().BeTrue();
		}

		[Fact]
		public async Task It_NotifiesSessionExpired_If_Refreshed_And_NoToken()
		{
			var sessionExpired = false;
			var refreshedToken = false;
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				refreshedToken = true;

				return Task.FromResult(default(TestToken));
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			refreshedToken.Should().BeTrue();
			sessionExpired.Should().BeTrue();
		}

		[Fact]
		public async Task It_NotifiesSessionExpired_If_Refreshed_And_Unauthorized()
		{
			var sessionExpired = false;
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");
			var refreshedAuthenticationToken = new TestToken("AccessToken2", "RefreshToken2");
			var authorizationHeaders = new List<AuthenticationHeaderValue>();

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
				=> Task.FromResult(refreshedAuthenticationToken);

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) =>
				{
					authorizationHeaders.Add(r.Headers.Authorization);

					var isUnauthorized = r.Headers.Authorization.Parameter == authenticationToken.AccessToken;

					return Task.FromResult(new HttpResponseMessage(isUnauthorized ? HttpStatusCode.Unauthorized : HttpStatusCode.Unauthorized));
				}));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			authorizationHeaders.Count.Should().Be(2);
			sessionExpired.Should().BeTrue();
		}

		[Fact]
		public async Task It_NotifiesSessionExpired_If_Refresh_Throws()
		{
			var sessionExpired = false;
			var refreshedToken = false;
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				refreshedToken = true;

				throw new TestException();
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await httpClient.GetAsync(DefaultRequestUri);

			refreshedToken.Should().BeTrue();
			sessionExpired.Should().BeTrue();
		}

		[Fact]
		public void It_Works_If_CircularReferences()
		{
			// The following circular reference is tested here:
			// AuthenticationTokenHandler -> IAuthenticationTokenProvider
			// IAuthenticationTokenProvider -> AuthenticationClient
			// AuthenticationClient -> AuthenticationTokenHandler

			void BuildServices(IServiceCollection s) => s
				// IAuthenticationTokenProvider is AuthenticationTokenProvider which depends on AuthenticationClient
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(
					sp.GetService<ILoggerFactory>(),
					(ct, r) => sp.GetRequiredService<AuthenticationClient>().GetToken(ct, r),
					(ct, r, t) => Task.CompletedTask
				))
				// AuthenticationTokenHandler depends on IAuthenticationTokenProvider
				.AddTransient<AuthenticationTokenHandler<TestToken>>();

			void BuildHttpClient(IHttpClientBuilder h) => h
				// AuthenticationClient depends on AuthenticationTokenHandler
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddTypedClient((c, s) => new AuthenticationClient(c));

			var serviceProvider = HttpClientTestsHelper.GetServiceProvider(s =>
			{
				BuildServices(s);
				BuildHttpClient(s.AddHttpClient(nameof(It_Works_If_CircularReferences)));
			});

			var tokenProvider = serviceProvider.GetService<IAuthenticationTokenProvider<TestToken>>();

			tokenProvider.Should().NotBeNull();

			var httpClient = serviceProvider.GetService<AuthenticationClient>();

			httpClient.Should().NotBeNull();
		}

		[Fact]
		public async Task It_NotifiesSessionExpired_If_Refreshed_And_Unauthorized_MultipeTimes()
		{
			var sessionExpired = false;
			var IsFirstRequestDone = false;

			var firstAuthenticationToken = new TestToken("AccessToken1", "RefreshToken1");
			var secondAuthenticationToken = new TestToken("AccessToken2", "RefreshToken2");
			var authorizationHeaders = new List<AuthenticationHeaderValue>();

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(IsFirstRequestDone ? secondAuthenticationToken : firstAuthenticationToken);

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
				=> Task.FromResult(default(TestToken));

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) =>
				{
					authorizationHeaders.Add(r.Headers.Authorization);

					return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
				}));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			// First time logged in.
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			// First time execute Get with unauthorized authentication token.
			await httpClient.GetAsync(DefaultRequestUri);

			authorizationHeaders.Count.Should().Be(1);
			sessionExpired.Should().BeTrue();

			// Second time logged in.
			IsFirstRequestDone = true;
			sessionExpired = false;
			authorizationHeaders.Clear();
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			// Second time execute Get with unauthorized authentication token.
			await httpClient.GetAsync(DefaultRequestUri);

			authorizationHeaders.Count.Should().Be(1);
			sessionExpired.Should().BeTrue();
		}

		[Fact]
		public async Task It_NotifiesSessionExpired_If_Refresh_Throws_MultipleTimes()
		{
			var sessionExpired = false;
			var refreshedToken = false;
			var IsFirstRequestDone = false;

			var firstAuthenticationToken = new TestToken("AccessToken1", "RefreshToken1");
			var secondAuthenticationToken = new TestToken("AccessToken2", "RefreshToken2");

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(IsFirstRequestDone ? secondAuthenticationToken : firstAuthenticationToken);

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				refreshedToken = true;

				throw new TestException();
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			// First time logged in.
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			// First time execute Get with unauthorized authentication token.
			await httpClient.GetAsync(DefaultRequestUri);

			refreshedToken.Should().BeTrue();
			sessionExpired.Should().BeTrue();

			// Second time logged in.
			sessionExpired = false;
			IsFirstRequestDone = true;
			refreshedToken = false;
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			// Second time execute Get with unauthorized authentication token.
			await httpClient.GetAsync(DefaultRequestUri);

			refreshedToken.Should().BeTrue();
			sessionExpired.Should().BeTrue();
		}

		[Fact]
		public async Task It_Handle_MultipleUnauthorizedRequest()
		{
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");
			var refreshedAuthenticationToken = new TestToken("AccessToken2", "RefreshToken2");
			var authorizationHeaders = new List<AuthenticationHeaderValue>();

			var hasNotRefreshed = true;
			TestToken currentRefreshToken = null;

			async Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
			{
				await Task.Delay(50);
				return hasNotRefreshed ? authenticationToken : currentRefreshToken;
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken unauthorizedToken)
				=> Task.CompletedTask;

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				currentRefreshToken = hasNotRefreshed ? refreshedAuthenticationToken : null;
				hasNotRefreshed = false;
				return Task.FromResult(currentRefreshToken);
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) =>
				{
					authorizationHeaders.Add(r.Headers.Authorization);

					var isUnauthorized = r.Headers.Authorization.Parameter != null && r.Headers.Authorization.Parameter == authenticationToken.AccessToken;

					return Task.FromResult(new HttpResponseMessage(isUnauthorized ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));
				}));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			// Simulate multiple unauthorized request.
			await Task.WhenAll(
				httpClient.GetAsync(DefaultRequestUri),
				httpClient.GetAsync(DefaultRequestUri),
				httpClient.GetAsync(DefaultRequestUri)
			);

			// Validate that there were 6 request in total 3 requests (unauthorized request + request with new token).
			authorizationHeaders.Count.Should().Be(6);
			for (var i = 0; i < 3; i++)
			{
				// Unauthorized request. (First three requests)
				authorizationHeaders.ElementAt(i).Parameter.Should().Be(authenticationToken.AccessToken);

				// request with new token. (Last three request, after one of the first three requets has succesfully refreshed its token)
				authorizationHeaders.ElementAt(3 + i).Parameter.Should().Be(refreshedAuthenticationToken.AccessToken);
			}
		}

		[Fact]
		public async Task It_Handle_MultipleUnauthorizedRequest_With_DifferentEndpoints()
		{
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");
			var refreshedAuthenticationToken = new TestToken("AccessToken2", "RefreshToken2");
			var authorizationHeaders = new List<AuthenticationHeaderValue>();

			var hasNotRefreshed = true;
			TestToken currentRefreshToken = null;

			async Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
			{
				await Task.Delay(50);
				return hasNotRefreshed ? authenticationToken : currentRefreshToken;
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken unauthorizedToken)
				=> Task.CompletedTask;

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				currentRefreshToken = hasNotRefreshed ? refreshedAuthenticationToken : null;
				hasNotRefreshed = false;
				return Task.FromResult(currentRefreshToken);
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) =>
				{
					authorizationHeaders.Add(r.Headers.Authorization);

					var isUnauthorized = r.Headers.Authorization.Parameter != null && r.Headers.Authorization.Parameter == authenticationToken.AccessToken;

					return Task.FromResult(new HttpResponseMessage(isUnauthorized ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));
				}));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClients = HttpClientTestsHelper.GetTestHttpClients(BuildServices, BuildHttpClient);

			httpClients.client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");
			httpClients.client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			// Simulate multiple unauthorized request.
			await Task.WhenAll(
				httpClients.client1.GetAsync(DefaultRequestUri),
				httpClients.client2.GetAsync(DefaultRequestUri)
			);

			// Validate that there were 2 request in total 2 requests (unauthorized request + request with new token).
			authorizationHeaders.Count.Should().Be(4);
			for (var i = 0; i < 2; i++)
			{
				// Unauthorized request. (First two requests)
				authorizationHeaders.ElementAt(i).Parameter.Should().Be(authenticationToken.AccessToken);

				// request with new token. (Last two request, after one of the first two requests has succesfully refreshed its token)
				authorizationHeaders.ElementAt(2 + i).Parameter.Should().Be(refreshedAuthenticationToken.AccessToken);
			}
		}

		[Fact]
		public async Task It_NotifiesSessionExpiredOnce_If_Refresh_Throws_MultipleTimesConcurrently()
		{
			var sessionExpired = false;
			var refreshedToken = false;
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");

			var sessionExpiredCount = 0;

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				refreshedToken = true;

				throw new TestException();
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;
				sessionExpiredCount++;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await Task.WhenAll(
				httpClient.GetAsync(DefaultRequestUri),
				httpClient.GetAsync(DefaultRequestUri)
			);

			refreshedToken.Should().BeTrue();
			sessionExpired.Should().BeTrue();
			sessionExpiredCount.Should().Be(1);
		}

		[Fact]
		public async Task It_NotifiesSessionExpiredOnce_If_Refresh_Throws_MultipleTimesConcurrently_With_DifferentEndpoints()
		{
			var sessionExpired = false;
			var refreshedToken = false;
			var authenticationToken = new TestToken("AccessToken1", "RefreshToken1");

			var sessionExpiredCount = 0;

			Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request)
				=> Task.FromResult(authenticationToken);

			Task<TestToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				refreshedToken = true;

				throw new TestException();
			}

			Task SessionExpired(CancellationToken ct, HttpRequestMessage request, TestToken token)
			{
				sessionExpired = true;
				sessionExpiredCount++;

				return Task.CompletedTask;
			}

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IAuthenticationTokenProvider<TestToken>>(sp => new ConcurrentAuthenticationTokenProvider<TestToken>(sp.GetService<ILoggerFactory>(), GetToken, SessionExpired, RefreshToken))
				.AddTransient<AuthenticationTokenHandler<TestToken>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<AuthenticationTokenHandler<TestToken>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClients = HttpClientTestsHelper.GetTestHttpClients(BuildServices, BuildHttpClient);

			httpClients.client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");
			httpClients.client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

			await Task.WhenAll(
				httpClients.client1.GetAsync(DefaultRequestUri),
				httpClients.client2.GetAsync(DefaultRequestUri)
			);

			refreshedToken.Should().BeTrue();
			sessionExpired.Should().BeTrue();
			sessionExpiredCount.Should().Be(1);
		}

		public class AuthenticationClient
		{
			public AuthenticationClient(HttpClient client) { }

			public async Task<TestToken> GetToken(CancellationToken ct, HttpRequestMessage request) => default(TestToken);
		}
	}
}
