using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Http.Handlers;
using Xunit;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class TestHandlerTests
	{
		private const string DefaultRequestUri = "http://wwww.test.com";

		[Fact]
		public async Task It_Returns_TestResponse()
		{
			var expectedResponse = new HttpResponseMessage();

			void BuildServices(IServiceCollection s) => s
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(expectedResponse)));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			var response = await httpClient.GetAsync(DefaultRequestUri);

			response.Should().Be(expectedResponse);
		}

		[Fact]
		public async Task It_Throws_Exception()
		{
			var expectedResponse = new HttpResponseMessage();

			void BuildServices(IServiceCollection s) => s
				.AddTransient(_ => new TestHandler((r, ct) => throw new TestException()));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			await Assert.ThrowsAsync<TestException>(() => httpClient.GetAsync(DefaultRequestUri));
		}
	}
}
