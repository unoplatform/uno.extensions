using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Serialization.Tests;

[TestClass]
public class ServiceCollectionExtensionsTests
{
	private IServiceProvider _services;

	[TestInitialize]
	public void InitializeTests()
	{
		var context = new HostBuilderContext(new Dictionary<object,object>());
		var services = new ServiceCollection();

#if WITH_AOT_TRIMMING
		services.AddJsonSerialization(context, TestJsonSerializerContext.Default);
#else   // !WITH_AOT_TRIMMING
		services.AddSystemTextJsonSerialization(context);
#endif  // WITH_AOT_TRIMMING

		_services = services.BuildServiceProvider();
	}

	[TestMethod]
	public void AddSystemTextJsonSerializationTest()
	{
		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}

	[TestMethod]
	public void StringSerializationWithRegisteredTypeInfoTest()
	{
		var serializer = _services.GetRequiredService<ISerializer>();
		serializer.Should().NotBeNull();

		// Test string serialization
		const string testValue = "test token value";
		var serialized = serializer.ToString(testValue, typeof(string));
		serialized.Should().NotBeNullOrEmpty();
		serialized.Should().Be("\"test token value\"");

		var deserialized = serializer.FromString(serialized, typeof(string));
		deserialized.Should().Be(testValue);

		// Test string[] serialization
		var testArray = new[] { "value1", "value2", "value3" };
		var serializedArray = serializer.ToString(testArray, typeof(string[]));
		serializedArray.Should().NotBeNullOrEmpty();
		serializedArray.Should().Be("[\"value1\",\"value2\",\"value3\"]");

		var deserializedArray = serializer.FromString(serializedArray, typeof(string[]));
		deserializedArray.Should().BeEquivalentTo(testArray);

		// Test bool serialization
		var serializedBool = serializer.ToString(true, typeof(bool));
		serializedBool.Should().NotBeNullOrEmpty();
		serializedBool.Should().Be("true");

		var deserializedBool = serializer.FromString(serializedBool, typeof(bool));
		deserializedBool.Should().Be(true);
	}

	[TestMethod]
	public void StringSerializerExtensionsWithRegisteredTypeInfoTest()
	{
		var serializer = _services.GetRequiredService<ISerializer>();
		serializer.Should().NotBeNull();

		// Test using extension method (same as ApplicationDataKeyValueStorage.Serialize<string>)
		const string testValue = "authentication_token_12345";
		var serialized = serializer.ToString(testValue);
		serialized.Should().NotBeNullOrEmpty();
		serialized.Should().Be("\"authentication_token_12345\"");

		var deserialized = serializer.FromString<string>(serialized);
		deserialized.Should().Be(testValue);
	}

	[TestMethod]
	public void AddSystemTextJsonSerialization_Works_With_AddJsonSerializationTypeInfoResolvers()
	{
		var context = new HostBuilderContext(new Dictionary<object,object>());
		var services = new ServiceCollection();

		services.AddJsonSerialization(context);
		services.AddJsonTypeInfo(TestJsonSerializerContext.Default);

		var originalServices = _services;
		try {
			_services = services.BuildServiceProvider();

			StringSerializationWithRegisteredTypeInfoTest();
			StringSerializerExtensionsWithRegisteredTypeInfoTest();
		}
		finally {
			_services = originalServices;
		}
	}
}


[JsonSourceGenerationOptions]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
internal sealed partial class TestJsonSerializerContext : JsonSerializerContext
{
}
