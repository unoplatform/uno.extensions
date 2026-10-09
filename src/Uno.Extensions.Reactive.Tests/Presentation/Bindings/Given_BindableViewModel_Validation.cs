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
using static Uno.Extensions.Reactive.Tests.ValidationTestHelper;

namespace Uno.Extensions.Reactive.Tests.Presentation.Bindings;

[TestClass]
public partial class Given_BindableViewModel_Validation : FeedUITests
{
	[TestMethod]
	public async Task When_PrimitiveStateValidated_Then_ErrorsChanged_And_HasErrors()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var errorsChanged = new List<string?>();
		var hasErrorsChanged = 0;
		var result = new ValidationResult("Required", new[] { "Name" });

		await ExecuteOnDispatcher(() =>
		{
			((INotifyDataErrorInfo)sut).ErrorsChanged += (snd, e) => errorsChanged.Add(e.PropertyName);
			sut.PropertyChanged += (snd, e) => hasErrorsChanged += e.PropertyName == nameof(BindableViewModelBase.HasErrors) ? 1 : 0;
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
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var result = new ValidationResult("Required");

		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitForAsync(async () => (await GetErrors(sut, "Name")).Any());

		(await GetErrors(sut, "Name")).Should().Equal(result);
		(await GetErrors(sut, null)).Should().BeEmpty();
		(await GetErrors(sut, "Other")).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_IdenticalResults_Then_NoErrorsChanged()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var errorsChanged = 0;
		await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut).ErrorsChanged += (snd, e) => errorsChanged++);

		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await WaitFor(() => errorsChanged is 1);
		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await sut.Model.Name.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Other") }), CT); // Marker
		await WaitForAsync(async () => (await GetErrors(sut, "Name")).Any(r => r.ErrorMessage == "Other"));

		(await ExecuteOnDispatcher(() => errorsChanged)).Should().Be(2);
	}

	[TestMethod]
	public async Task When_ValueSetFromView_Then_ValidationStillDelivered()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Validated_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		await WaitForAsync(async () => (await GetErrors(sut, "Name")).Any()); // Initial value is empty, so it's invalid

		await ExecuteOnDispatcher(() => sut.Name = "valid");
		await WaitForAsync(async () => !(await GetErrors(sut, "Name")).Any());

		await ExecuteOnDispatcher(() => sut.Name = "");
		await WaitForAsync(async () => (await GetErrors(sut, "Name")).Any());

		(await GetErrors(sut, "Name")).Single().ErrorMessage.Should().Be("Name is required");
		(await ExecuteOnDispatcher(() => sut.Name)).Should().Be("", "validation must never block the value");
	}

	[TestMethod]
	public async Task When_RecordMemberValidated_Then_ErrorsRoutedToBindableRecord()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
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
	public async Task When_NestedRecordMemberValidated_Then_ErrorsRoutedToNestedBindableRecord()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var result = new ValidationResult("Street is required", new[] { "Address.Street" });

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitForAsync(async () => (await GetErrors(sut.Person.Address, "Street")).Any());

		(await GetErrors(sut.Person.Address, "Street")).Should().Equal(result);
		(await GetErrors(sut.Person, "Address")).Should().BeEmpty("the error is about the street, not the whole address");
		(await ExecuteOnDispatcher(() => sut.Person.Address.HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => sut.Person.HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeTrue();
	}

	[TestMethod]
	public async Task When_RecordMemberOfRecordTypeValidated_Then_ErrorOnMember_And_EntityLevelOnNested()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var result = new ValidationResult("Address is invalid", new[] { "Address" });

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitForAsync(async () => (await GetErrors(sut.Person, "Address")).Any());

		(await GetErrors(sut.Person, "Address")).Should().Equal(result);
		(await GetErrors(sut.Person.Address, null)).Should().Equal(result);
		(await GetErrors(sut.Person.Address, "")).Should().Equal(result);
	}

	[TestMethod]
	public async Task When_RecordValidatedWithoutMemberNames_Then_PropertyLevel_And_EntityLevel()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Record_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var result = new ValidationResult("Person is invalid");

		await sut.Model.Person.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitForAsync(async () => (await GetErrors(sut, "Person")).Any());

		(await GetErrors(sut, "Person")).Should().Equal(result);
		(await GetErrors(sut.Person, null)).Should().Equal(result);
		(await GetErrors(sut.Person, "FirstName")).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_RecordMemberSetFromView_Then_ValidationDelivered()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_ValidatedRecord_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved

		await ExecuteOnDispatcher(() => sut.Person.FirstName = "");
		await WaitForAsync(async () => (await GetErrors(sut.Person, "FirstName")).Any());

		(await ExecuteOnDispatcher(() => sut.Person.FirstName)).Should().Be("");

		await ExecuteOnDispatcher(() => sut.Person.FirstName = "John");
		await WaitForAsync(async () => !(await GetErrors(sut.Person, "FirstName")).Any());
	}

	[TestMethod]
	public async Task When_RecordDeclaresHasErrors_Then_INotifyDataErrorInfoStillValid()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_RecordWithHasErrors_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var result = new ValidationResult("Name is required", new[] { "Name" });

		await sut.Model.Form.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await WaitForAsync(async () => (await GetErrors(sut.Form, "Name")).Any());

		(await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut.Form).HasErrors)).Should().BeTrue();
		(await ExecuteOnDispatcher(() => sut.Form.HasErrors)).Should().BeFalse("the member of the record hides the one of the bindable");
	}

	[TestMethod]
	public async Task When_NoValidation_Then_NoErrorsChanged_And_NoStoreAllocated()
	{
		await using var sut = await ExecuteOnDispatcher(() => new When_Primitive_ViewModel()); // Created on the UI thread, so the dispatcher of the VM is resolved
		var errorsChanged = 0;
		await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut).ErrorsChanged += (snd, e) => errorsChanged++);

		await sut.Model.Name.SetAsync("updated", CT);
		await WaitForAsync(async () => await ExecuteOnDispatcher(() => sut.Name) == "updated");

		(await ExecuteOnDispatcher(() => errorsChanged)).Should().Be(0);
		(await ExecuteOnDispatcher(() => sut.HasErrors)).Should().BeFalse();
		typeof(BindableViewModelBase)
			.GetField("_validation", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(sut)
			.Should()
			.BeNull();
	}

	private async Task<ValidationResult[]> GetErrors(object bindable, string? propertyName)
		=> await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)bindable).GetErrors(propertyName).Cast<ValidationResult>().ToArray());

	/// <remarks>The predicate is evaluated on the UI thread, where the events are raised.</remarks>
	private async Task WaitFor(Func<bool> predicate)
		=> await WaitForAsync(async () => await ExecuteOnDispatcher(predicate));

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
}
