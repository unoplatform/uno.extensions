using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Generator;

/// <summary>
/// Tests to verify that generated bindable types are flagged with the BindableAttribute.
/// This ensures better performance and trimming support by avoiding reflection at runtime.
/// </summary>
[TestClass]
public partial class Given_BindableAttribute_Then_Flagged : FeedUITests
{
	[TestMethod]
	public void ViewModelGenerator_GeneratesBindableAttributeForRecords()
	{
		// When ViewModelGenerator_2 generates a bindable wrapper for a record type,
		// it should include the BindableAttribute to avoid reflection at runtime.
		// ViewModelGenerator_2 generates types named Bindable{RecordName}ViewModel
		var bindableType = typeof(BindableTestRecordViewModel);
		
		Assert.IsNotNull(bindableType, "The generated bindable type should exist");
		
		var bindableAttribute = bindableType.GetCustomAttribute<BindableAttribute>();
		Assert.IsNotNull(bindableAttribute, "The generated bindable type should have BindableAttribute");
		Assert.AreEqual(typeof(TestRecord), bindableAttribute.Model, "The BindableAttribute should reference the correct model type");
	}

	[TestMethod]
	public void ViewModelGenTool_3_GeneratesBindableAttribute()
	{
		// ViewModelGenTool_3 generates types named {ModelName}ViewModel (for ViewModels with partial class)
		var bindableType = typeof(TestViewModel);
		
		Assert.IsNotNull(bindableType, "The generated bindable type should exist");
		
		var bindableAttribute = bindableType.GetCustomAttribute<BindableAttribute>();
		Assert.IsNotNull(bindableAttribute, "The generated bindable type should have BindableAttribute");
		Assert.AreEqual(typeof(TestModel), bindableAttribute.Model, "The BindableAttribute should reference the correct model type");
	}

	[TestMethod]
	public void ViewModelGenerator_GeneratesBindableAttributeForNestedRecords()
	{
		// Test that records containing other records also generate the BindableAttribute
		var bindableType = typeof(BindableTestRecordWithListViewModel);
		
		Assert.IsNotNull(bindableType, "The generated bindable type should exist");
		
		var bindableAttribute = bindableType.GetCustomAttribute<BindableAttribute>();
		Assert.IsNotNull(bindableAttribute, "The generated bindable type should have BindableAttribute");
		Assert.AreEqual(typeof(TestRecordWithList), bindableAttribute.Model, "The BindableAttribute should reference the correct model type");
	}
}

// Test model for ViewModelGenTool_3 (generates view models from partial classes with feeds)
// This will trigger generation of bindable wrappers for the records it references
public partial class TestModel
{
	public IFeed<TestRecord> Record => Feed.Async(async ct => new TestRecord("Test", 42));
	public IFeed<TestRecordWithList> RecordWithList => Feed.Async(async ct => new TestRecordWithList(ImmutableList<TestRecord>.Empty));
}

// Test record for ViewModelGenerator_2 (generates bindable wrappers for records)
public partial record TestRecord(string Name, int Value);

// Test record with nested list to verify complex scenarios
public partial record TestRecordWithList(ImmutableList<TestRecord> Items);
