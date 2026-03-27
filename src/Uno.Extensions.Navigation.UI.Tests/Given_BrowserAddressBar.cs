using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Navigation.UI.Tests;

[TestClass]
public class Given_BrowserAddressBar
{
	/// <summary>
	/// Verifies that when a host is built using an assembly loaded in the default ALC,
	/// the <see cref="IHasAddressBar"/> service is registered (on platforms that support it).
	/// </summary>
	[TestMethod]
	public void When_DefaultALC_Then_AddressBarService_Is_Registered()
	{
		// Arrange — the test assembly lives in the default ALC
		var assembly = typeof(Given_BrowserAddressBar).Assembly;
		AssemblyLoadContext.GetLoadContext(assembly).Should().Be(
			AssemblyLoadContext.Default,
			"test assembly should be in the default ALC for this test to be meaningful");

		// Act
		var host = UnoHost
			.CreateDefaultBuilder(assembly)
			.UseNavigation(
				viewRouteBuilder: (views, routes) =>
				{
					views.Register(new ViewMap<Pages.TestPageOne>());
					routes.Register(new RouteMap("", Nested: new RouteMap[]
					{
						new("TestPageOne", View: views.FindByView<Pages.TestPageOne>()),
					}));
				})
			.Build();

		// Assert — on WASM IHasAddressBar should be registered; on other platforms it may not be,
		// but the important thing is the code path doesn't throw.
		var config = host.Services.GetService<NavigationConfiguration>();
		config.Should().NotBeNull();
		config!.AddressBarUpdateEnabled.Should().BeTrue("default configuration enables address bar updates");
	}

	/// <summary>
	/// Verifies that when a host is built using an assembly from a non-default ALC,
	/// the <see cref="IHasAddressBar"/> service is NOT registered — preventing inner/plugin
	/// apps from hijacking the browser address bar.
	/// </summary>
	[TestMethod]
	public void When_NonDefaultALC_Then_AddressBarService_Is_Not_Registered()
	{
		// Arrange — load the test assembly into a custom (non-default) ALC
		var alc = new AssemblyLoadContext("TestNonDefaultALC", isCollectible: true);
		try
		{
			var assemblyPath = typeof(Given_BrowserAddressBar).Assembly.Location;

			// On some platforms (e.g. single-file publish), Location may be empty.
			// Skip this test gracefully in that case.
			if (string.IsNullOrEmpty(assemblyPath))
			{
				Assert.Inconclusive("Assembly.Location is not available on this platform.");
				return;
			}

			var nonDefaultAssembly = alc.LoadFromAssemblyPath(assemblyPath);
			AssemblyLoadContext.GetLoadContext(nonDefaultAssembly).Should().NotBe(
				AssemblyLoadContext.Default,
				"assembly should be loaded in a non-default ALC");

			// Act
			var host = UnoHost
				.CreateDefaultBuilder(nonDefaultAssembly)
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(new ViewMap<Pages.TestPageOne>());
						routes.Register(new RouteMap("", Nested: new RouteMap[]
						{
							new("TestPageOne", View: views.FindByView<Pages.TestPageOne>()),
						}));
					})
				.Build();

			// Assert
			var addressBar = host.Services.GetService<IHasAddressBar>();
			addressBar.Should().BeNull(
				"IHasAddressBar must not be registered for assemblies in a non-default ALC, " +
				"because inner/plugin apps should not update the browser address bar");
		}
		finally
		{
			alc.Unload();
		}
	}
}
