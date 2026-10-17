using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests.Browser;

/// <summary>
/// Navigation keeps the browser's address bar and Back button in sync on WebAssembly, and only there
/// (unoplatform/uno.extensions#3253).
/// </summary>
/// <remarks>
/// Asserted at the host rather than by navigating: the WebAssembly runtime-test harness cannot
/// activate a window, which navigation initialization requires.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_BrowserNavigation
{
	private static readonly string[] BrowserServices = { "BrowserAddressBarService", "BackButtonService" };

	[TestMethod]
	public void When_UseNavigation_Then_Browser_Sync_Hosted_Only_In_Browser()
	{
		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_BrowserNavigation).Assembly)
			.UseNavigation()
			.Build();

		var hosted = host.Services.GetServices<IHostedService>().Select(service => service.GetType().Name).ToArray();

		if (OperatingSystem.IsBrowser())
		{
			hosted.Should().Contain(BrowserServices, "navigation must update the address bar and answer the browser's Back button");
		}
		else
		{
			hosted.Should().NotContain(BrowserServices, "only the browser has an address bar and Back button to keep in sync");
		}
	}
}
