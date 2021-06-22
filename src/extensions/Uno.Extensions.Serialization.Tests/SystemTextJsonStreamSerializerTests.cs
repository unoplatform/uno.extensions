﻿using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Serialization.Tests
{
    [TestClass]
    public class SystemTextJsonStreamSerializerTests
    {
        private const string SimpleText = "Hello World!";

        private ISerializer Serializer { get; set; }

        [TestInitialize]
        public void InitializeTests()
        {
            Serializer = new SystemTextJsonStreamSerializer();
        }
 
        [TestMethod]
        public void SimpleClassSerializationTest()
        {
            var entity = CreateSimpleClassInstance();
            TestEntitySerialization<SimpleClass>(entity);
        }

        [TestMethod]
        public void SimpleRecordSerializationTest()
        {
            var entity = CreateSimpleRecordInstance();
            TestEntitySerialization<SimpleRecord>(entity);
        }

        [TestMethod]
        public void SimpleClassStringifyTest()
        {
            var entity = CreateSimpleClassInstance();
            TestEntityStringify<SimpleClass>(entity);
        }

        [TestMethod]
        public void SimpleRecordStringifyTest()
        {
            var entity = CreateSimpleRecordInstance();
            TestEntityStringify<SimpleRecord>(entity);
        }

        private void TestEntitySerialization<T>(object entity)
            where T:ISimpleText
        {
            using (var ms = new MemoryStream())
            {
                Serializer.WriteToStream(ms, entity, typeof(T));
                ms.Flush();

                // Reset the stream so we can read
                ms.Seek(0, SeekOrigin.Begin);

                var clonedEntity = Serializer.ReadFromStream(ms, typeof(T));
                Assert.IsInstanceOfType(clonedEntity, typeof(T));
                Assert.AreNotSame(entity, clonedEntity);
                Assert.AreEqual(((T)entity).SimpleTextProperty, ((T)clonedEntity).SimpleTextProperty);

                // Reset the stream so we can write again
                ms.Seek(0, SeekOrigin.Begin);

                Serializer.WriteToStream(ms, clonedEntity, typeof(T));
                ms.Flush();

                // Reset the stream so we can read
                ms.Seek(0, SeekOrigin.Begin);
                var anotherClone = Serializer.ReadFromStream(ms, typeof(T));

                Assert.IsInstanceOfType(anotherClone, typeof(T));
                Assert.AreNotSame(clonedEntity, anotherClone);
                Assert.AreEqual(((T)clonedEntity).SimpleTextProperty, ((T)anotherClone).SimpleTextProperty);
            }
        }


        private void TestEntityStringify<T>(object entity)
            where T : ISimpleText
        {
            var stringValue = Serializer.ToString(entity, typeof(T));
            var clonedEntity = Serializer.FromString(stringValue, typeof(T));
            Assert.IsInstanceOfType(clonedEntity, typeof(T));
            Assert.AreNotSame(entity, clonedEntity);
            Assert.AreEqual(((T)entity).SimpleTextProperty, ((T)clonedEntity).SimpleTextProperty);
        }

       public static ISimpleText CreateSimpleClassInstance()
        {
            return new SimpleClass { SimpleTextProperty = SimpleText + "Class" };
        }

        public static ISimpleText CreateSimpleRecordInstance()
        {
            return new SimpleRecord (SimpleTextProperty : SimpleText + "Record");
        }
    }

    public class SimpleClass : ISimpleText
    {
        public string SimpleTextProperty { get; set; }
    }

    public record SimpleRecord(string SimpleTextProperty) : ISimpleText;


    public interface ISimpleText
    {
        string SimpleTextProperty { get; }
    }



}

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}
