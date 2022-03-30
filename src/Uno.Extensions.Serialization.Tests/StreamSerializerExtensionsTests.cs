using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Serialization.Tests;

[TestClass]
public class StreamSerializerExtensionsTests
{
	private SystemTextJsonStreamSerializer Serializer { get; set; }

	[TestInitialize]
	public void InitializeTests()
	{
		Serializer = new SystemTextJsonStreamSerializer();
	}

	[TestMethod]
	public void ToAndFromStreamTest()
	{
		var classEntity = SystemTextJsonStreamSerializerTests.CreateSimpleClassInstance();
		var stream = Serializer.ToStream(classEntity);
		var cloneClass = Serializer.FromStream<SimpleClass>(stream);
		VerifyEntity(classEntity, cloneClass);
		var anotherCloneClass = Serializer.FromStream<SimpleClass>(stream);
		VerifyEntity(cloneClass, anotherCloneClass);

		var recordEntity = SystemTextJsonStreamSerializerTests.CreateSimpleRecordInstance();
		stream = Serializer.ToStream(recordEntity);
		var cloneRecord = Serializer.FromStream<SimpleRecord>(stream);
		VerifyEntity(recordEntity, cloneRecord);
		var anotherCloneRecord = Serializer.FromStream<SimpleRecord>(stream);
		VerifyEntity(cloneRecord, anotherCloneRecord);
	}

	[TestMethod]
	public void ReadWriteToStreamTest()
	{
		var classEntity = SystemTextJsonStreamSerializerTests.CreateSimpleClassInstance() as SimpleClass;
		using var ms = new MemoryStream();
		Serializer.WriteToStream(ms, classEntity);
		var pos = ms.Position;
		ms.Seek(0, SeekOrigin.Begin);
		var cloneClass = Serializer.ReadFromStream<SimpleClass>(ms);
		VerifyEntity(classEntity, cloneClass);
		Assert.AreEqual(pos, ms.Position);

		ms.Seek(0, SeekOrigin.Begin);
		Serializer.WriteToStream(ms, (object)classEntity);
		pos = ms.Position;
		ms.Seek(0, SeekOrigin.Begin);
		cloneClass = Serializer.ReadFromStream<SimpleClass>(ms);
		VerifyEntity(classEntity, cloneClass);
		Assert.AreEqual(pos, ms.Position);
	}

	[TestMethod]
	public void ToFromStringTest()
	{
		var classEntity = SystemTextJsonStreamSerializerTests.CreateSimpleClassInstance() as SimpleClass;
		var stringValue = Serializer.ToString(classEntity);
		var cloneClass = Serializer.FromString<SimpleClass>(stringValue);
		VerifyEntity(classEntity, cloneClass);

		var recordEntity = SystemTextJsonStreamSerializerTests.CreateSimpleRecordInstance() as SimpleRecord;
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
