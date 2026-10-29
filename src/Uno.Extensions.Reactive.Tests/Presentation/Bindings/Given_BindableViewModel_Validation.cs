using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Presentation.Bindings;

/// <remarks>View models are created on the UI thread, so their dispatcher is resolved.</remarks>
[TestClass]
public partial class Given_BindableViewModel_Validation : FeedUITests
{
	[TestMethod]
	public async Task When_PrimitiveStateValidated_Then_ErrorsChanged_And_HasErrors()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel());
		var errorsChanged = new List<string?>();
		var hasErrorsChanged = 0;
		var result = new ValidationResult("Required", new[] { "Name" });

		await ExecuteOnDispatcher(() =>
		{
			((INotifyDataErrorInfo)sut).ErrorsChanged += (snd, e) => errorsChanged.Add(e.PropertyName);
			sut.PropertyChanged += (snd, e) => hasErrorsChanged += e.PropertyName == nameof(When_Validated_ViewModel.HasErrors) ? 1 : 0;
		});

		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(() => errorsChanged.Count is 1);

		(await GetErrors(sut, "Name")).Should().Equal(result);
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut).HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => errorsChanged.ToArray())).Should().Equal("Name");
		(await ExecuteOnDispatcher(() => hasErrorsChanged)).Should().Be(1);

		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(null), CT);
		await WaitFor(() => errorsChanged.Count is 2);

		(await GetErrors(sut, "Name")).Should().BeEmpty();
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeFalse();
		(await ExecuteOnDispatcher(() => hasErrorsChanged)).Should().Be(2);
	}

	[TestMethod]
	public async Task When_PrimitiveStateValidatedWithoutMemberNames_Then_PropertyLevelErrors()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel());
		var result = new ValidationResult("Required");

		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => (await GetErrors(sut, "Name")).Any());

		(await GetErrors(sut, "Name")).Should().Equal(result);
		(await GetErrors(sut, null)).Should().BeEmpty();
		(await GetErrors(sut, "Other")).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_IdenticalResults_Then_NoErrorsChanged()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel());
		var errorsChanged = 0;
		await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut).ErrorsChanged += (snd, e) => errorsChanged++);

		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await WaitFor(() => errorsChanged is 1);
		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Other") }), CT); // Marker
		await WaitFor(async () => (await GetErrors(sut, "Name")).Any(r => r.ErrorMessage == "Other"));

		(await ExecuteOnDispatcher(() => errorsChanged)).Should().Be(2);
	}

	[TestMethod]
	public async Task When_ValueSetFromView_Then_ValidationStillDelivered()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Validated_ViewModel());
		await WaitFor(async () => (await GetErrors(sut, "Name")).Any()); // Initial value is empty, so it's invalid

		await ExecuteOnDispatcher(() => sut.Name = "valid");
		await WaitFor(async () => !(await GetErrors(sut, "Name")).Any());

		await ExecuteOnDispatcher(() => sut.Name = "");
		await WaitFor(async () => (await GetErrors(sut, "Name")).Any());

		(await GetErrors(sut, "Name")).Single().ErrorMessage.Should().Be("Name is required");
		(await ExecuteOnDispatcher(() => sut.Name)).Should().Be("", "validation must never block the value");
	}

	[TestMethod]
	public async Task When_RecordMemberValidated_Then_ErrorsRoutedToBindableRecord()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel());
		var person = (INotifyDataErrorInfo)sut.Person;
		var personErrorsChanged = new List<string?>();
		var personHasErrorsChanged = 0;
		var result = new ValidationResult("First name is required", new[] { "FirstName" });

		await ExecuteOnDispatcher(() =>
		{
			person.ErrorsChanged += (snd, e) => personErrorsChanged.Add(e.PropertyName);
			sut.Person.PropertyChanged += (snd, e) => personHasErrorsChanged += e.PropertyName == nameof(Bindable<object>.HasErrors) ? 1 : 0;
		});

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(() => personErrorsChanged.Count is 1);

		(await GetErrors(sut.Person, "FirstName")).Should().Equal(result);
		(await GetErrors(sut, "Person")).Should().BeEmpty();
		(await ExecuteOnDispatcher(() => sut.Person.HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeTrue("the VM aggregates the errors of its sub-bindables");
		(await ExecuteOnDispatcher(() => personErrorsChanged.ToArray())).Should().Equal("FirstName");
		(await ExecuteOnDispatcher(() => personHasErrorsChanged)).Should().Be(1);

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(null), CT);
		await WaitFor(() => personErrorsChanged.Count is 2);

		(await GetErrors(sut.Person, "FirstName")).Should().BeEmpty();
		(await ExecuteOnDispatcher(() => sut.Person.HasErrors)).Should().BeFalse();
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_ResultTargetsUnknownMember_Then_EntityLevelErrorOfBindableRecord()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel());
		var result = new ValidationResult("First name is required", new[] { "FristName" }); // Typo: no binding would ever query it

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => (await GetErrors(sut.Person, null)).Any());

		(await GetErrors(sut.Person, null)).Should().Equal(result);
		(await GetErrors(sut.Person, "FristName")).Should().BeEmpty();
		(await ExecuteOnDispatcher(() => sut.Person.HasErrors)).Should().BeTrue();
	}

	[TestMethod]
	public async Task When_ResultTargetsAmbiguousMember_Then_DoesNotThrow()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_RecordWithHiddenMember_ViewModel());
		var result = new ValidationResult("Code is invalid", new[] { nameof(ValidationDerived.Code) }); // Declared by the record and, with another type, by its base

		await sut.Model.Value.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => await ExecuteOnDispatcher(() => sut.Value.HasErrors));

		(await GetErrors(sut.Value, nameof(ValidationDerived.Code))).Should().Equal(result);
	}

	[TestMethod]
	public async Task When_NestedRecordMemberValidated_Then_ErrorsRoutedToNestedBindableRecord()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel());
		var result = new ValidationResult("Street is required", new[] { "Address.Street" });

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => (await GetErrors(sut.Person.Address, "Street")).Any());

		(await GetErrors(sut.Person.Address, "Street")).Should().Equal(result);
		(await GetErrors(sut.Person, "Address")).Should().BeEmpty("the error is about the street, not the whole address");
		(await ExecuteOnDispatcher(() => sut.Person.Address.HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => sut.Person.HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeTrue();
	}

	[TestMethod]
	public async Task When_RecordMemberOfRecordTypeValidated_Then_ErrorOnMember_And_EntityLevelOnNested()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel());
		var result = new ValidationResult("Address is invalid", new[] { "Address" });

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => (await GetErrors(sut.Person, "Address")).Any());

		(await GetErrors(sut.Person, "Address")).Should().Equal(result);
		(await GetErrors(sut.Person.Address, null)).Should().Equal(result);
		(await GetErrors(sut.Person.Address, "")).Should().Equal(result);
	}

	[TestMethod]
	public async Task When_RecordValidatedWithoutMemberNames_Then_PropertyLevel_And_EntityLevel()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel());
		var result = new ValidationResult("Person is invalid");

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => (await GetErrors(sut, "Person")).Any());

		(await GetErrors(sut, "Person")).Should().Equal(result);
		(await GetErrors(sut.Person, null)).Should().Equal(result);
		(await GetErrors(sut.Person, "FirstName")).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_RecordMemberSetFromView_Then_ValidationDelivered()
	{
		var initialized = false;
		await using var sut = await ExecuteOnDispatcher(() =>
		{
			var vm = new When_ValidatedRecord_ViewModel();
			vm.PropertyChanged += (snd, e) => initialized |= e.PropertyName == nameof(vm.Person);
			return vm;
		});

		// The view model pushes the first value of the state to the view asynchronously:
		// a value set from the view before that would be overwritten by the initial value (while the state keeps the value set from the view).
		await WaitFor(() => initialized);
		await ExecuteOnDispatcher(() => sut.Person.FirstName = "");
		await WaitFor(async () => (await GetErrors(sut.Person, "FirstName")).Any());

		(await ExecuteOnDispatcher(() => sut.Person.FirstName)).Should().Be("");

		await ExecuteOnDispatcher(() => sut.Person.FirstName = "John");
		await WaitFor(async () => !(await GetErrors(sut.Person, "FirstName")).Any());
	}

	[TestMethod]
	public async Task When_RecordDeclaresHasErrors_Then_INotifyDataErrorInfoStillValid()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_RecordWithHasErrors_ViewModel());
		var result = new ValidationResult("Name is required", new[] { "Name" });

		await sut.Model.Form.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => (await GetErrors(sut.Form, "Name")).Any());

		(await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut.Form).HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => sut.Form.HasErrors)).Should().BeFalse("the member of the record hides the one of the bindable");
	}

	[TestMethod]
	public async Task When_NoValidation_Then_NoErrorsChanged_And_NoStoreAllocated()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel());
		var errorsChanged = 0;
		await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut).ErrorsChanged += (snd, e) => errorsChanged++);

		await sut.Model.Name.SetAsync("updated", CT);
		await WaitFor(async () => await ExecuteOnDispatcher(() => sut.Name) == "updated");

		(await ExecuteOnDispatcher(() => errorsChanged)).Should().Be(0);
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeFalse();
		GetStore<BindableViewModelBase>(sut).Should().BeNull();
	}

	[TestMethod]
	public async Task When_RecordStateWithoutValidation_Then_NoStoreAllocated()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel());

		await sut.Model.Person.UpdateAsync(_ => new ValidationPerson("Jane", new ValidationAddress("Second street")), CT);
		await WaitFor(async () => await ExecuteOnDispatcher(() => sut.Person.FirstName) == "Jane");

		// The bindable records subscribe to the validation of their owner, but the store must be allocated only once results are received.
		GetStore<BindableViewModelBase>(sut).Should().BeNull();
		GetStore<Bindable<ValidationPerson>>(sut.Person).Should().BeNull();
		GetStore<Bindable<ValidationAddress>>(sut.Person.Address).Should().BeNull();
	}

	[TestMethod]
	public async Task When_RecordStateValidatedAfterSubBindablesCreated_Then_ErrorsRoutedToNestedBindableRecord()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel());
		_ = await ExecuteOnDispatcher(() => sut.Person.Address); // Sub-bindables subscribe before any result is received
		var result = new ValidationResult("Street is required", new[] { "Address.Street" });

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitFor(async () => (await GetErrors(sut.Person.Address, "Street")).Any());

		(await GetErrors(sut.Person.Address, "Street")).Should().Equal(result);
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeTrue();
	}

	private static object? GetStore<TOwner>(object owner)
		=> ((LazyBindableValidationErrors)typeof(TOwner).GetField("_validation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!).Store;

	[TestMethod]
	public async Task When_PropertyWithoutValidation_Then_NoStoreAllocatedForSubBindables()
	{
		await using var owner = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel());

		// Like an item of a list (cf. BindableEnumerable), the property does not provide any validation result.
		var item = await ExecuteOnDispatcher(() =>
		{
			var person = new ValidationPerson("John", new ValidationAddress("Main street"));
			return new BindableValidationPersonViewModel(new BindablePropertyInfo<ValidationPerson>(
				owner,
				"Item",
				(Feed.Async(async ct => person), onUpdated => onUpdated(person)),
				setter: null));
		});

		GetStore<Bindable<ValidationPerson>>(item).Should().BeNull("the sub-bindables of the item cannot receive any validation result");
		GetStore<Bindable<ValidationAddress>>(item.Address).Should().BeNull();
	}

	private async Task<ValidationResult[]> GetErrors(object bindable, string? propertyName)
		=> await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)bindable).GetErrors(propertyName).Cast<ValidationResult>().ToArray());

	/// <remarks>The predicate is evaluated on the UI thread, where the events are raised.</remarks>
	private async Task WaitFor(Func<bool> predicate)
		=> await WaitFor(async () => await ExecuteOnDispatcher(predicate));

	private static async Task WaitFor(Func<Task<bool>> predicate)
	{
		for (var i = 0; i < 500; i++)
		{
			if (await predicate())
			{
				return;
			}

			await Task.Delay(10);
		}

		throw new TimeoutException();
	}

	public partial class When_Primitive_Model
	{
		public IState<string> Name => State.Value(this, () => "initial");
	}

	public partial class When_Validated_Model
	{
		public IState<string> Name => State.Value(this, () => "")
			.Validate(async (name, ct) => string.IsNullOrWhiteSpace(name)
				? new[] { new ValidationResult("Name is required", new[] { nameof(Name) }) }
				: Enumerable.Empty<ValidationResult>());
	}

	public partial class When_Record_Model
	{
		public IState<ValidationPerson> Person => State.Value(this, () => new ValidationPerson("John", new ValidationAddress("Main street")));
	}

	public partial class When_ValidatedRecord_Model
	{
		public IState<ValidationPerson> Person => State.Value(this, () => new ValidationPerson("John", new ValidationAddress("Main street")))
			.Validate(async (person, ct) => string.IsNullOrWhiteSpace(person.FirstName)
				? new[] { new ValidationResult("First name is required", new[] { nameof(ValidationPerson.FirstName) }) }
				: Enumerable.Empty<ValidationResult>());
	}

	public partial class When_RecordWithHasErrors_Model
	{
		public IState<ValidationForm> Form => State.Value(this, () => new ValidationForm("", false));
	}

	public partial record ValidationPerson(string FirstName, ValidationAddress Address);

	public partial record ValidationAddress(string Street);

	public partial record ValidationForm(string Name, bool HasErrors);

	public partial class When_RecordWithHiddenMember_Model
	{
		public IState<ValidationDerived> Value => State.Value(this, () => new ValidationDerived());
	}

	public record ValidationBase
	{
		public int Code { get; init; }
	}

	public partial record ValidationDerived : ValidationBase
	{
		public new string Code { get; init; } = "";
	}
}
