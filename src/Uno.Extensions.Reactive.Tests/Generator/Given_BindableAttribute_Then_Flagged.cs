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
		// When ViewModelGenerator_1 or ViewModelGenerator_2 generates a bindable wrapper for a record type,
		// it should include the BindableAttribute to avoid reflection at runtime.
		// The generated type should be named Bindable{RecordName}ViewModel for ViewModelGenerator_2
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

// Test record for ViewModelGenerator (used by ViewModelGenTool_3)
public partial record TestRecord(string Name, int Value);

// Test record with nested list to verify complex scenarios
public partial record TestRecordWithList(ImmutableList<TestRecord> Items);

// Test model/viewmodel pair for ViewModelGenTool_3
public partial class TestModel
{
	public IFeed<string> Name => Feed.Async(async ct => "Test");
	public IFeed<int> Value => Feed.Async(async ct => 42);
}
