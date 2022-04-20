using System;
using System.Linq;
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

		Assert.IsNotNull(bindable.AFeedField as IState<string>);
		Assert.IsNotNull(bindable.AStateField as IState<string>);
		Assert.IsNotNull(bindable.AListFeedField as IListState<string>); // This should actually be a IListState
		Assert.IsNotNull(bindable.AListStateField as IListState<string>); // This should actually be a IListState
		Assert.IsNotNull(bindable.AListFeedField as ICollectionView);
		Assert.IsNotNull(bindable.AListStateField as ICollectionView);
		Assert.IsNotNull(bindable.ACustomFeedField as IState<string>);

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

		Assert.IsNotNull(bindable.AFeedProperty as IState<string>);
		Assert.IsNotNull(bindable.AStateProperty as IState<string>);
		Assert.IsNotNull(bindable.AListFeedProperty as IListFeed<string>); // This should actually be a IListState
		Assert.IsNotNull(bindable.AListStateProperty as IListFeed<string>); // This should actually be a IListState
		Assert.IsNotNull(bindable.AListFeedProperty as ICollectionView);
		Assert.IsNotNull(bindable.AListStateProperty as ICollectionView);
		Assert.IsNotNull(bindable.ACustomFeedProperty as IState<string>);

		bindable.AParameterLessMethod();
		bindable.AParameterizedMethod("arg1", 42);

		(string result1, int result2) = bindable.AParameterLessMethodReturningATuple();
		Assert.AreEqual("AParameterLessMethodReturningATuple", result1);
		Assert.AreEqual(42, result2);

		(result1, result2) = bindable.AParameterizedMethodReturningATuple("AParameterizedMethodReturningATuple", 43);
		Assert.AreEqual("AParameterizedMethodReturningATuple", result1);
		Assert.AreEqual(43, result2);
	}
}
