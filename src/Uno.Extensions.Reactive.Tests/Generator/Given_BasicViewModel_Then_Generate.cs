using System;
using System.Linq;
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
			defaultAWeirdRecordInput: default(MyWeirdRecord)!);

		var bindableCtor5 = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel(
			aParameterToNotBeAParameterLessCtor2: (int)0);
	}

	[TestMethod]
	public void Test_PublicMembers()
	{
		var mysSubRecord = new MySubRecord("prop1", 42);
		var myWeirdRecord = new MyWeirdRecord();
		var myRecord = new MyRecord("prop1", 42, mysSubRecord, myWeirdRecord);

		var bindable = new Given_BasicViewModel_Then_Generate__ViewModel.BindableGiven_BasicViewModel_Then_Generate__ViewModel(
			aParameterToNotBeAParameterLessCtor1: (short)42,
			defaultAnInput: "anInput",
			defaultAReadWriteInput: "aReadWriteInput",
			defaultARecordInput: myRecord,
			defaultAWeirdRecordInput: myWeirdRecord);

		Assert.IsNotNull(bindable.Model as Given_BasicViewModel_Then_Generate__ViewModel);

		Assert.AreEqual<string>("anInput", bindable.AnInput);
		bindable.AnInput = "hasSetter";

		Assert.AreEqual<string>("aReadWriteInput", bindable.AReadWriteInput);
		bindable.AReadWriteInput = "hasSetter";

		Assert.AreEqual<MyRecord>(myRecord, bindable.ARecordInput.GetValue()!);

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

		Assert.IsNotNull(bindable.AFeedField as IState<string>);
		Assert.IsNotNull(bindable.AStateField as IState<string>);
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
