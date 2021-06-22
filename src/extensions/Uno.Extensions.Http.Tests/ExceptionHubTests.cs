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
	public class ExceptionHubTests
	{
		private const string DefaultRequestUri = "http://wwww.test.com";

		[Fact]
		public async Task It_Reports_Exception()
		{
			var expectedException = new TestException();
			var exceptionHub = new ExceptionHub();
			var reportedException = default(Exception);

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IExceptionHub>(exceptionHub)
				.AddTransient<ExceptionHubHandler>()
				.AddTransient(_ => new TestHandler((r, ct) => throw expectedException));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<ExceptionHubHandler>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			void OnExceptionReported(object sender, Exception e)
			{
				reportedException = e;
			}

			exceptionHub.OnExceptionReported += OnExceptionReported;

			await Assert.ThrowsAsync<TestException>(() => httpClient.GetAsync(DefaultRequestUri));

			reportedException.Should().Be(expectedException);
		}
	}
}
