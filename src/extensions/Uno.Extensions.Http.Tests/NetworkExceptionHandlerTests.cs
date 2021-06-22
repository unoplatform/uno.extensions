using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Http.Handlers;
using Xunit;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class NetworkExceptionHandlerTests
	{
		private const string DefaultRequestUri = "http://wwww.test.com";

		[Fact]
		public async Task It_Throws_NoNetworkException_When_ExceptionAndNoNetwork()
		{
			void BuildServices(IServiceCollection s) => s
				.AddSingleton<INetworkAvailabilityChecker>(new NetworkAvailabilityChecker(ct => Task.FromResult(false))) // No network
				.AddTransient(_ => new TestHandler((r, ct) => throw new TestException())) // Exception
				.AddTransient<NetworkExceptionHandler>();

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<NetworkExceptionHandler>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			await Assert.ThrowsAsync<NoNetworkException>(() => httpClient.GetAsync(DefaultRequestUri));
		}

		[Fact]
		public async Task It_Throws_CustomNetworkException_When_ExceptionAndNoNetwork()
		{
			void BuildServices(IServiceCollection s) => s
				.AddSingleton<INetworkAvailabilityChecker>(new NetworkAvailabilityChecker(ct => Task.FromResult(false))) // No network
				.AddSingleton<INetworkExceptionFactory>(new CustomNetworkExceptionFactory())
				.AddTransient(_ => new TestHandler((r, ct) => throw new TestException())) // Exception
				.AddTransient<NetworkExceptionHandler>();

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<NetworkExceptionHandler>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			await Assert.ThrowsAsync<CustomNetworkException>(() => httpClient.GetAsync(DefaultRequestUri));
		}

		[Fact]
		public async Task It_Doesnt_Throw_NoNetworkException_When_ExceptionAndNetwork()
		{
			void BuildServices(IServiceCollection s) => s
				.AddSingleton<INetworkAvailabilityChecker>(new NetworkAvailabilityChecker(ct => Task.FromResult(true))) // Has network
				.AddTransient(_ => new TestHandler((r, ct) => throw new TestException())) // Exception
				.AddTransient<NetworkExceptionHandler>();

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<NetworkExceptionHandler>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			await Assert.ThrowsAsync<TestException>(() => httpClient.GetAsync(DefaultRequestUri));
		}

		[Fact]
		public async Task It_Doesnt_Throw_NoNetworkException_When_NoExceptionAndNetwork()
		{
			var expectedResponse = new HttpResponseMessage();

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<INetworkAvailabilityChecker>(new NetworkAvailabilityChecker(ct => Task.FromResult(true))) // Has network
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(expectedResponse))) // No exception
				.AddTransient<NetworkExceptionHandler>();

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<NetworkExceptionHandler>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			var response = await httpClient.GetAsync(DefaultRequestUri);

			response.Should().Be(expectedResponse);
		}

		private class CustomNetworkExceptionFactory : INetworkExceptionFactory
		{
			public Exception CreateNetworkException(Exception innerException)
				=> new CustomNetworkException();
		}

		private class CustomNetworkException : Exception { }
	}
}
