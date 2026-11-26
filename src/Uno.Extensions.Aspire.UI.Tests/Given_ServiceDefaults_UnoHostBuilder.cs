using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.UI.Tests;

/// <summary>
/// The address Aspire passes wins over the app's settings files on Uno's <see cref="Hosting.HostBuilder"/>, the
/// builder apps get. It holds because that builder runs every host configuration step before it enumerates the app
/// configuration steps; these tests fail if that order changes.
/// </summary>
[TestClass]
public class Given_ServiceDefaults_UnoHostBuilder
{
	private const string EnvironmentName = "services__unoaspiretest__http__0";
	private const string SettingsKey = "services:unoaspiretest:http:0";
	private const string InjectedAddress = "http://localhost:5171";
	private const string SettingsAddress = "https://api.example.com";

	[TestMethod]
	public void When_CalledBeforeTheSettings_Then_TheInjectedAddressWins() =>
		ResolveAddress(builder => AddSettings(builder.UseServiceDefaults())).Should().Be(InjectedAddress);

	[TestMethod]
	public void When_CalledAfterTheSettings_Then_TheInjectedAddressWins() =>
		ResolveAddress(builder => AddSettings(builder).UseServiceDefaults()).Should().Be(InjectedAddress);

	private static IHostBuilder AddSettings(IHostBuilder builder) =>
		builder.ConfigureAppConfiguration((_, config) =>
			config.AddInMemoryCollection(new Dictionary<string, string?> { [SettingsKey] = SettingsAddress }));

	private static string? ResolveAddress(Func<IHostBuilder, IHostBuilder> configure)
	{
		Environment.SetEnvironmentVariable(EnvironmentName, InjectedAddress);
		try
		{
			using var host = configure(new Hosting.HostBuilder()).Build();
			return host.Services.GetRequiredService<IConfiguration>()[SettingsKey];
		}
		finally
		{
			Environment.SetEnvironmentVariable(EnvironmentName, null);
		}
	}
}
