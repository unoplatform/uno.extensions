using System;
using System.Collections.Generic;
using System.IO;
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
	}

	[TestMethod]
	public void AddSystemTextJsonSerializationTest()
	{
		var context = new HostBuilderContext(new Dictionary<object,object>());
		var services = new ServiceCollection();
		services.AddSystemTextJsonSerialization(context);
		_services = services.BuildServiceProvider();

		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}

	[TestMethod]
	public void StringSerializationWithRegisteredTypeInfoTest()
	{
		// This test validates the fix for the JsonSerializerIsReflectionDisabled issue
		// where serializing strings would fail in AOT scenarios without registered type info
		var context = new HostBuilderContext(new Dictionary<object, object>());
		var services = new ServiceCollection();
		services.AddSystemTextJsonSerialization(context);
		var serviceProvider = services.BuildServiceProvider();

		var serializer = serviceProvider.GetRequiredService<ISerializer>();
		serializer.Should().NotBeNull();

		// Test string serialization
		const string testValue = "test token value";
		var serialized = serializer.ToString(testValue, typeof(string));
		serialized.Should().NotBeNullOrEmpty();

		var deserialized = serializer.FromString(serialized, typeof(string));
		deserialized.Should().Be(testValue);

		// Test string[] serialization
		var testArray = new[] { "value1", "value2", "value3" };
		var serializedArray = serializer.ToString(testArray, typeof(string[]));
		serializedArray.Should().NotBeNullOrEmpty();

		var deserializedArray = serializer.FromString(serializedArray, typeof(string[]));
		deserializedArray.Should().BeEquivalentTo(testArray);

		// Test bool serialization
		var serializedBool = serializer.ToString(true, typeof(bool));
		serializedBool.Should().NotBeNullOrEmpty();

		var deserializedBool = serializer.FromString(serializedBool, typeof(bool));
		deserializedBool.Should().Be(true);
	}

	[TestMethod]
	public void StringSerializerExtensionsWithRegisteredTypeInfoTest()
	{
		// This test validates that the SerializerExtensions.ToString<T> method works
		// for string types with the registered type info (same path as KeyValueStorage)
		var context = new HostBuilderContext(new Dictionary<object, object>());
		var services = new ServiceCollection();
		services.AddSystemTextJsonSerialization(context);
		var serviceProvider = services.BuildServiceProvider();

		var serializer = serviceProvider.GetRequiredService<ISerializer>();
		serializer.Should().NotBeNull();

		// Test using extension method (same as ApplicationDataKeyValueStorage.Serialize<string>)
		const string testValue = "authentication_token_12345";
		var serialized = serializer.ToString(testValue);
		serialized.Should().NotBeNullOrEmpty();

		var deserialized = serializer.FromString<string>(serialized);
		deserialized.Should().Be(testValue);
	}
}
