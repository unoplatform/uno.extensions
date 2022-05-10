using System;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public class Given_BasicViewModel_Then_Generate : FeedUITests
{
	// Those are mostly compilation tests!

	[TestMethod]
	public void Test_Constructors()
	{
		var bindableCtor1 = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel();

		var bindableCtor2 = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel(aRandomService: "aRandomService");

		var bindableCtor3 = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel(
			anExternalInput: default(IFeed<string>)!,
			anExternalReadWriteInput: default(IState<string>)!,
			anExternalRecordInput: default(IFeed<MyRecord>)!,
			anExternalWeirdRecordInput: default(IFeed<MyWeirdRecord>)!);

		var bindableCtor4 = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel(
			aParameterToNotBeAParameterLessCtor1: (short)0,
			defaultAnInput: default(string)!,
			defaultAReadWriteInput: default(string)!,
			defaultARecordInput: default(MyRecord)!,
			defaultAWeirdRecordInput: default(MyWeirdRecord)!,
			defaultARecordWithAValuePropertyInput: default(MyRecordWithAValueProperty)!,
			defaultAnInputConflictingWithAProperty: (int)42);

		var bindableCtor5 = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel(
			aParameterToNotBeAParameterLessCtor2: (int)0);
	}

	[TestMethod]
	public void Test_PublicMembers()
	{
		var mysSubRecord = new MySubRecord("prop1", 42);
		var myWeirdRecord = new MyWeirdRecord();
		var myRecord = new MyRecord("prop1", 42, mysSubRecord, myWeirdRecord);
		var myRecordWithAValueProperty = new MyRecordWithAValueProperty("42");

		var bindable = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel(
			aParameterToNotBeAParameterLessCtor1: (short)42,
			defaultAnInput: "anInput",
			defaultAReadWriteInput: "aReadWriteInput",
			defaultARecordInput: myRecord,
			defaultAWeirdRecordInput: myWeirdRecord,
			defaultARecordWithAValuePropertyInput: myRecordWithAValueProperty,
			defaultAnInputConflictingWithAProperty: (int)42);

		Assert.IsNotNull(bindable.Model as Given_BasicViewModel_Then_Generate__ViewModel);

		Assert.AreEqual<string>("anInput", bindable.AnInput);
		bindable.AnInput = "hasSetter";

		Assert.AreEqual<string>("aReadWriteInput", bindable.AReadWriteInput);
		bindable.AReadWriteInput = "hasSetter";

		Assert.AreEqual<MyRecord>(myRecord, bindable.ARecordInput.GetValue()!);

		// De normalized properties
		((string)bindable.ARecordInput.Property1).ToString();
		((int)bindable.ARecordInput.Property2).ToString();
		((BindableMySubRecord)bindable.ARecordInput.Property3).ToString();
		((BindableMyWeirdRecord)bindable.ARecordInput.Property4).ToString();
		((string)bindable.ARecordInput.Property3.Prop1).ToString();
		((int)bindable.ARecordInput.Property3.Prop2).ToString();
		((string)bindable.ARecordInput.Property4.ReadWriteProperty).ToString();
		((string)bindable.ARecordInput.Property4.ReadInitProperty ).ToString();
		((string)bindable.ARecordInput.Property4.ReadOnlyProperty ).ToString();
		bindable.ARecordInput.Property4.WriteOnlyProperty = "";
		bindable.ARecordInput.Property4.InitOnlyProperty = "";
		((string?)bindable.ARecordInput.Property4.ANullableProperty)?.ToString();

		// Value properties
		((MySubRecord)bindable.ARecordInput.Property3.Value).ToString();
		((MyWeirdRecord)bindable.ARecordInput.Property4.Value).ToString();

		((BindableMyWeirdRecord)bindable.AWeirdRecordInput).ToString();
		((MyWeirdRecord)bindable.AWeirdRecordInput.Value).ToString();

		((BindableMyRecordWithAValueProperty)bindable.ARecordWithAValuePropertyInput).ToString();
		((string)bindable.ARecordWithAValuePropertyInput.Value).ToString();

		Assert.AreEqual<MyWeirdRecord>(myWeirdRecord, bindable.AWeirdRecordInput.GetValue()!);

		Assert.IsNotNull(bindable.ATriggerInput as IAsyncCommand);

		Assert.IsNotNull(bindable.ATypedTriggerInput as IAsyncCommand);

		bindable.Model.AField = "AField_SetFromVM";
		Assert.AreEqual("AField_SetFromVM", bindable.AField);
		bindable.AField = "AField_SetFromBindable";
		Assert.AreEqual("AField_SetFromBindable", bindable.Model.AField);

		bindable.Model.AnInternalField = "AnInternalField_SetFromVM";
		Assert.AreEqual("AnInternalField_SetFromVM", bindable.AnInternalField);
		bindable.AnInternalField = "AnInternalField_SetFromBindable";
		Assert.AreEqual("AnInternalField_SetFromBindable", bindable.Model.AnInternalField);

		bindable.Model.AProtectedInternalField = "AProtectedInternalField_SetFromVM";
		Assert.AreEqual("AProtectedInternalField_SetFromVM", bindable.AProtectedInternalField);
		bindable.AProtectedInternalField = "AProtectedInternalField_SetFromBindable";
		Assert.AreEqual("AProtectedInternalField_SetFromBindable", bindable.Model.AProtectedInternalField);

		Assert.AreEqual(bindable.Model.AnInputConflictingWithAProperty, "AnInputConflictingWithAProperty");
		bindable.AnInputConflictingWithAProperty = 42; // This should be of type 'int'

		Assert.IsTrue(bindable.GetType().GetProperty("AFeedField")?.PropertyType == typeof(string));
		Assert.IsTrue(bindable.GetType().GetProperty("AStateField")?.PropertyType == typeof(string));
		Assert.IsTrue(bindable.GetType().GetProperty("ACustomFeedField")?.PropertyType == typeof(string));

		Assert.IsNotNull(bindable.ARecordFeedField as IFeed<MyRecord>);
		Assert.IsNotNull(bindable.ARecordFeedField as BindableMyRecord);
		Assert.IsFalse(bindable.ARecordFeedField.CanWrite);
		//Assert.AreEqual(bindable.ARecordFeedField.Property1, "ARecordFeedField"); // Source feed is async

		Assert.IsNotNull(bindable.ARecordStateField as IFeed<MyRecord>);
		Assert.IsNotNull(bindable.ARecordStateField as BindableMyRecord);
		Assert.IsTrue(bindable.ARecordStateField.CanWrite);
		//Assert.AreEqual(bindable.ARecordStateField.Property1, "ARecordStateField"); // Source feed is async

		Assert.IsNotNull(bindable.AListFeedField as IListState<string>);
		Assert.IsNotNull(bindable.AListFeedField as ICollectionView);

		Assert.IsNotNull(bindable.AListStateField as IListState<string>);
		Assert.IsNotNull(bindable.AListStateField as ICollectionView);

		bindable.Model.AProperty = "AProperty_SetFromVM";
		Assert.AreEqual("AProperty_SetFromVM", bindable.AProperty);
		bindable.AProperty = "AProperty_SetFromBindable";
		Assert.AreEqual("AProperty_SetFromBindable", bindable.Model.AProperty);

		bindable.Model.AnInternalProperty = "AnInternalProperty_SetFromVM";
		Assert.AreEqual("AnInternalProperty_SetFromVM", bindable.AnInternalProperty);
		bindable.AnInternalProperty = "AnInternalProperty_SetFromBindable";
		Assert.AreEqual("AnInternalProperty_SetFromBindable", bindable.Model.AnInternalProperty);

		bindable.Model.AProtectedInternalProperty = "AProtectedInternalProperty_SetFromVM";
		Assert.AreEqual("AProtectedInternalProperty_SetFromVM", bindable.AProtectedInternalProperty);
		bindable.AProtectedInternalProperty = "AProtectedInternalProperty_SetFromBindable";
		Assert.AreEqual("AProtectedInternalProperty_SetFromBindable", bindable.Model.AProtectedInternalProperty);

		Assert.AreEqual(bindable.Model.AReadOnlyProperty, bindable.AReadOnlyProperty);

		bindable.ASetOnlyProperty = "hasSetter";

		Assert.IsTrue(bindable.GetType().GetProperty("AFeedProperty")?.PropertyType == typeof(string));
		Assert.IsTrue(bindable.GetType().GetProperty("AStateProperty")?.PropertyType == typeof(string));
		Assert.IsTrue(bindable.GetType().GetProperty("ACustomFeedProperty")?.PropertyType == typeof(string));

		Assert.IsNotNull(bindable.ARecordFeedProperty as IFeed<MyRecord>);
		Assert.IsNotNull(bindable.ARecordFeedProperty as BindableMyRecord);
		Assert.IsFalse(bindable.ARecordFeedProperty.CanWrite);
		//Assert.AreEqual(bindable.ARecordFeedProperty.Property1, "ARecordFeedProperty"); // Source feed is async

		Assert.IsNotNull(bindable.ARecordStateProperty as IFeed<MyRecord>);
		Assert.IsNotNull(bindable.ARecordStateProperty as BindableMyRecord);
		Assert.IsTrue(bindable.ARecordStateProperty.CanWrite);
		//Assert.AreEqual(bindable.ARecordStateProperty.Property1, "ARecordStateProperty"); // Source feed is async

		Assert.IsNotNull(bindable.AListFeedProperty as IListState<string>);
		Assert.IsNotNull(bindable.AListFeedProperty as ICollectionView);
		Assert.IsNotNull(bindable.AListStateProperty as IListState<string>);
		Assert.IsNotNull(bindable.AListStateProperty as ICollectionView);

		Assert.IsNotNull(bindable.AParameterLessMethod as ICommand);
		Assert.IsNotNull(bindable.AParameterLessMethodReturningATuple as ICommand);

		bindable.AParameterizedMethod("arg1", 42);

		var (result1, result2) = bindable.AParameterizedMethodReturningATuple("AParameterizedMethodReturningATuple", 43);
		Assert.AreEqual("AParameterizedMethodReturningATuple", result1);
		Assert.AreEqual(43, result2);
	}
}
