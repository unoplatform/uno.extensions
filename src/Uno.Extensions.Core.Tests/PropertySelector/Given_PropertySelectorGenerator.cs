using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Edition;

namespace Uno.Extensions.Core.Tests.PropertySelector;

[TestClass]
public class Given_PropertySelectorGenerator
{
	[TestMethod]
	public void When_Simple()
		=> Test(new ValueRecord("hello"), "world", [PropertySelector] (e) => e.Value);

	[TestMethod]
	public void When_NestedValue()
		=> Test(new RootRecord<ValueRecord>(new ("hello")), "world", [PropertySelector] (e) => e.Nested.Value);

	[TestMethod]
	public void When_NullableValue_Propagation()
		=> Test(new NullableValueRecord("hello"), "world", [PropertySelector] (e) => e.Value);

	[TestMethod]
	public void When_NullableValue_PropagationWithNull()
		=> Test(new NullableValueRecord(default), "hello world", [PropertySelector] (e) => e.Value);

	[TestMethod]
	public void When_NullableValue_Suppression()
		=> Test(new NullableValueRecord("hello"), "world", [PropertySelector] (e) => e.Value!);

	[TestMethod]
	public void When_NullableValue_SuppressionWithNull()
		=> Test(new NullableValueRecord(default), "world", [PropertySelector] (e) => e.Value!);

	[TestMethod]
	public void When_NullableNestedValue_Propagation()
		=> Test(new RootNullableRecord<NullableValueRecord>(new("hello")), "world", [PropertySelector] (e) => e.Nested?.Value);

	[TestMethod]
	public void When_NullableNestedValue_PropagationWithNull()
		=> Test(new RootNullableRecord<NullableValueRecord>(default), "hello world", [PropertySelector] (e) => e.Nested?.Value);

	[TestMethod]
	public void When_NullableNestedValue_Suppression()
		=> Test(new RootNullableRecord<NullableValueRecord>(new("hello")), "world", [PropertySelector] (e) => e.Nested!.Value);

	[TestMethod]
	[ExpectedException(typeof(NullReferenceException))]
	public void When_NullableNestedValue_SuppressionWithNull()
		=> GetAccessor<RootNullableRecord<NullableValueRecord>, string?>([PropertySelector] (e) => e.Nested!.Value).Get(new (default));

	[TestMethod]
	public void When_NullableNestedValue_DoubleSuppression()
		=> Test(new RootNullableRecord<NullableValueRecord>(new("hello")), "world", [PropertySelector] (e) => e.Nested!.Value!);

	[TestMethod]
	[ExpectedException(typeof(NullReferenceException))]
	public void When_NullableNestedValue_DoubleSuppressionWithIntermediateNull()
		=> GetAccessor<RootNullableRecord<NullableValueRecord>, string>([PropertySelector] (e) => e.Nested!.Value!).Get(new(default));

	[TestMethod]
	public void When_NullableNestedValue_DoubleSuppressionWithFinalNull()
		=> Test(new RootNullableRecord<NullableValueRecord>(new (default)), "hello world", [PropertySelector] (e) => e.Nested!.Value!);

	public record ValueRecord(string Value);
	public record NullableValueRecord(string? Value);
	public record RootRecord<T>(T Nested);
	public record RootNullableRecord<T>(T? Nested);

	private IValueAccessor<TEntity, TValue> GetAccessor<TEntity, TValue>(PropertySelector<TEntity, TValue> selector, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1)
	{
		var accessor = PropertySelectors.Get(selector, nameof(selector), path, line);

		accessor.Should().NotBeNull(because: "has we have 'path' and 'line', we should be able to retrieve the selector.");

		return accessor;
	}

	public void Test<TEntity, TValue>(TEntity entity, TValue value, PropertySelector<TEntity, TValue> selector, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1)
	{
		var accessor = PropertySelectors.Get(selector, nameof(selector), path, line);

		accessor.Should().NotBeNull(because: "has we have 'path' and 'line', we should be able to retrieve the selector.");

		var originalValue = accessor.Get(entity);
		var updatedEntity = accessor.Set(entity, value);

		accessor.Get(entity).Should().Be(originalValue, because: "the original entity should not have been modified");
		accessor.Get(updatedEntity).Should().Be(value, because: "we should be able to 'get' the value we just 'set'");
	}

		
}
