using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Serialization.Tests;

/// <summary>
/// Guards that the process-wide <see cref="JsonSerializationOptions.DefaultSerializerOptions"/> template
/// is never handed to a serializer directly.
/// </summary>
/// <remarks>
/// <c>System.Text.Json</c> caches a <c>JsonTypeInfo</c> per (de)serialized type on the options instance.
/// A downstream host that loads previewed apps into their own collectible <c>AssemblyLoadContext</c>s
/// would otherwise accumulate every app type's metadata on the shared static, pinning those types (and
/// their ALCs) for the process lifetime. Callers must receive a host-scoped copy instead.
/// </remarks>
[TestClass]
public class Given_JsonSerializationOptions
{
	[TestMethod]
	public void When_CreateSerializerOptions_Then_ReturnsFreshCopyNotTemplate()
	{
		var first = JsonSerializationOptions.CreateSerializerOptions();
		var second = JsonSerializationOptions.CreateSerializerOptions();

		first.Should().NotBeSameAs(JsonSerializationOptions.DefaultSerializerOptions,
			"the shared template must never be handed out directly");
		second.Should().NotBeSameAs(first, "each caller must get its own instance");

		// The copy carries the template's configuration.
		first.AllowTrailingCommas.Should().Be(JsonSerializationOptions.DefaultSerializerOptions.AllowTrailingCommas);
		first.NumberHandling.Should().Be(JsonSerializationOptions.DefaultSerializerOptions.NumberHandling);
	}

	[TestMethod]
	public void When_NoOptionsConfigured_Then_FallbackDoesNotReturnTemplate()
	{
		var services = new ServiceCollection().BuildServiceProvider();

		var options = services.GetJsonSerializationOptions();

		options.Should().NotBeSameAs(JsonSerializationOptions.DefaultSerializerOptions,
			"the fallback must be a host-scoped copy so reflection-based JsonTypeInfo caching " +
			"does not accumulate app types on the shared template");
	}

	[TestMethod]
	public void When_TwoHostsRegisterSerialization_Then_EachGetsIndependentOptions()
	{
		// Two independent hosts (as a downstream host creates per previewed app).
		var firstHost = BuildSerializerServices();
		var secondHost = BuildSerializerServices();

		var firstOptions = firstHost.GetRequiredService<JsonSerializerOptions>();
		var secondOptions = secondHost.GetRequiredService<JsonSerializerOptions>();

		// Each host has its own options, and neither is the shared template. This is what confines
		// System.Text.Json's per-type JsonTypeInfo cache to a host, so serializing one app's types
		// cannot pin them (or their ALC) via the process-wide template.
		firstOptions.Should().NotBeSameAs(JsonSerializationOptions.DefaultSerializerOptions);
		secondOptions.Should().NotBeSameAs(JsonSerializationOptions.DefaultSerializerOptions);
		firstOptions.Should().NotBeSameAs(secondOptions, "each host must own its serializer options");

		// Serializing through one host must still work off its own copy.
		var serializer = firstHost.GetRequiredService<ISerializer>();
		serializer.ToString(new HostedType { Value = 42 }, typeof(HostedType)).Should().Contain("42");
	}

	private static IServiceProvider BuildSerializerServices()
	{
		var context = new HostBuilderContext(new System.Collections.Generic.Dictionary<object, object>());
		var services = new ServiceCollection();

		// Register HostedType through source generation so the serializer sanity check below still works
		// when System.Text.Json reflection is disabled (the AOT/trimming test config), matching the repo
		// rule to always use generated serializers. Mirrors the split in ServiceCollectionExtensionsTests.
#if WITH_AOT_TRIMMING
		services.AddJsonSerialization(context, HostedTypeContext.Default);
#else
		services.AddSystemTextJsonSerialization(context);
#endif
		return services.BuildServiceProvider();
	}
}

public sealed class HostedType
{
	public int Value { get; set; }
}

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(HostedType))]
internal sealed partial class HostedTypeContext : JsonSerializerContext
{
}
