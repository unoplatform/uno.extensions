using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Serialization.Tests;

[TestClass]
public class SerializerExtensionsTests
{
	private SystemTextJsonSerializer Serializer { get; set; }

	[TestInitialize]
	public void InitializeTests()
	{
		// Register serialization services to support AOT scenarios
		var services = new ServiceCollection();
		
		// First register SystemTextJsonSerializer as singleton for ISerializer
		services.AddSingleton<SystemTextJsonSerializer>();
		services.AddSingleton<ISerializer>(sp => sp.GetRequiredService<SystemTextJsonSerializer>());
		
		// Register the generic serializer factory
		services.AddSingleton(typeof(ISerializer<>), typeof(SystemTextJsonGeneratedSerializer<>));
		
		// Register type info for test types
		services.AddJsonTypeInfo(TestTypesJsonSerializerContext.Default.SimpleClass);
		services.AddJsonTypeInfo(TestTypesJsonSerializerContext.Default.SimpleRecord);
		
		var serviceProvider = services.BuildServiceProvider();
		Serializer = serviceProvider.GetRequiredService<SystemTextJsonSerializer>();
	}

	[TestMethod]
	public void ToAndFromStreamTest()
	{
		var classEntity = SystemTextJsonSerializerTests.CreateSimpleClassInstance() as SimpleClass;
		var stream = Serializer.ToStream(classEntity);
		stream.Seek(0, SeekOrigin.Begin);
		var cloneClass = Serializer.FromStream<SimpleClass>(stream);
		VerifyEntity(classEntity, cloneClass);
		stream.Seek(0, SeekOrigin.Begin);
		var anotherCloneClass = Serializer.FromStream<SimpleClass>(stream);
		VerifyEntity(cloneClass, anotherCloneClass);

		var recordEntity = SystemTextJsonSerializerTests.CreateSimpleRecordInstance() as SimpleRecord;
		stream = Serializer.ToStream(recordEntity);
		stream.Seek(0, SeekOrigin.Begin);
		var cloneRecord = Serializer.FromStream<SimpleRecord>(stream);
		VerifyEntity(recordEntity, cloneRecord);
		stream.Seek(0, SeekOrigin.Begin);
		var anotherCloneRecord = Serializer.FromStream<SimpleRecord>(stream);
		VerifyEntity(cloneRecord, anotherCloneRecord);
	}

	[TestMethod]
	public void ReadWriteToStreamTest()
	{
		var classEntity = SystemTextJsonSerializerTests.CreateSimpleClassInstance() as SimpleClass;
		using var ms = new MemoryStream();
		Serializer.ToStream(ms, classEntity);
		var pos = ms.Position;
		ms.Seek(0, SeekOrigin.Begin);
		var cloneClass = Serializer.FromStream<SimpleClass>(ms);
		VerifyEntity(classEntity, cloneClass);
		Assert.AreEqual(pos, ms.Position);

		ms.Seek(0, SeekOrigin.Begin);
		Serializer.ToStream<SimpleClass>(ms, classEntity);
		pos = ms.Position;
		ms.Seek(0, SeekOrigin.Begin);
		cloneClass = Serializer.FromStream<SimpleClass>(ms);
		VerifyEntity(classEntity, cloneClass);
		Assert.AreEqual(pos, ms.Position);
	}

	[TestMethod]
	public void ToFromStringTest()
	{
		var classEntity = SystemTextJsonSerializerTests.CreateSimpleClassInstance() as SimpleClass;
		var stringValue = Serializer.ToString(classEntity);
		var cloneClass = Serializer.FromString<SimpleClass>(stringValue);
		VerifyEntity(classEntity, cloneClass);

		var recordEntity = SystemTextJsonSerializerTests.CreateSimpleRecordInstance() as SimpleRecord;
		stringValue = Serializer.ToString(recordEntity);
		var cloneRecord = Serializer.FromString<SimpleRecord>(stringValue);
		VerifyEntity(recordEntity, cloneRecord);
	}

	private void VerifyEntity(object expectedEntity, object actualEntity)
	{
		Assert.IsInstanceOfType(expectedEntity, typeof(ISimpleText));
		Assert.IsInstanceOfType(actualEntity, typeof(ISimpleText));
		var expected = (ISimpleText)expectedEntity;
		var actual = (ISimpleText)actualEntity;
		Assert.AreNotSame(expected, actual);
		Assert.AreEqual(expected.SimpleTextProperty, actual.SimpleTextProperty);
	}
}
