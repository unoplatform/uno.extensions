using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_Validation
{
	// The validator runs off the UI thread, then the results are dispatched back to it: give it more than the default 1 sec.
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[TestMethod]
	public async Task When_TwoWayBoundTextBoxSetInvalid_Then_ErrorsChangedOnUIThread(CancellationToken ct)
	{
		await using var vm = new MvuxValidationViewModel();
		var textBox = new TextBox();
		textBox.SetBinding(TextBox.TextProperty, new Binding
		{
			Path = new PropertyPath(nameof(MvuxValidationViewModel.Name)),
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		var ui = new StackPanel { DataContext = vm, Children = { textBox } };
		var errorsChanged = new List<(string? propertyName, bool hasThreadAccess)>();
		var hasErrorsChanged = new List<bool>();
		var propertyChanged = new List<string?>();
		((INotifyDataErrorInfo)vm).ErrorsChanged += (snd, e) => errorsChanged.Add((e.PropertyName, textBox.DispatcherQueue.HasThreadAccess));
		vm.PropertyChanged += (snd, e) =>
		{
			propertyChanged.Add(e.PropertyName);
			if (e.PropertyName == nameof(MvuxValidationViewModel.HasErrors))
			{
				hasErrorsChanged.Add(textBox.DispatcherQueue.HasThreadAccess);
			}
		};

		await UIHelper.Load(ui, ct);
		await TestHelper.WaitFor(() => textBox.Text == "initial", _timeout, ct);
		await WaitForInitialSync(propertyChanged, nameof(MvuxValidationViewModel.Name), ct);

		GetErrors(vm, "Name").Should().BeEmpty("the initial value is valid");
		vm.HasErrors.Should().BeFalse();

		// Act: invalid input
		textBox.Text = "";
		await TestHelper.WaitFor(() => GetErrors(vm, "Name").Any(), _timeout, ct);

		GetErrors(vm, "Name").Should().Equal("Name is required");
		vm.HasErrors.Should().BeTrue();
		((INotifyDataErrorInfo)vm).HasErrors.Should().BeTrue();
		vm.Name.Should().Be("", "validation must never block the value pushed by the binding");
		errorsChanged.Should().Equal(("Name", true));
		hasErrorsChanged.Should().Equal(true);

		// Act: valid input
		textBox.Text = "John";
		await TestHelper.WaitFor(() => !GetErrors(vm, "Name").Any(), _timeout, ct);

		vm.HasErrors.Should().BeFalse();
		vm.Name.Should().Be("John");
		errorsChanged.Should().Equal(("Name", true), ("Name", true));
		hasErrorsChanged.Should().Equal(true, true);
	}

	[TestMethod]
	public async Task When_TwoWayBoundTextBoxOnRecordMemberSetInvalid_Then_ErrorsReportedByBindableRecord(CancellationToken ct)
	{
		await using var vm = new MvuxValidationViewModel();
		var textBox = new TextBox();
		textBox.SetBinding(TextBox.TextProperty, new Binding
		{
			Path = new PropertyPath($"{nameof(MvuxValidationViewModel.Person)}.{nameof(MvuxValidationPerson.FirstName)}"),
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		var ui = new StackPanel { DataContext = vm, Children = { textBox } };

		// The source object of the leaf of the binding path (i.e. the one WinUI queries for errors) is the generated bindable of the record.
		var person = (INotifyDataErrorInfo)vm.Person;
		var personErrorsChanged = new List<(string? propertyName, bool hasThreadAccess)>();
		var vmErrorsChanged = new List<string?>();
		var propertyChanged = new List<string?>();
		person.ErrorsChanged += (snd, e) => personErrorsChanged.Add((e.PropertyName, textBox.DispatcherQueue.HasThreadAccess));
		((INotifyDataErrorInfo)vm).ErrorsChanged += (snd, e) => vmErrorsChanged.Add(e.PropertyName);
		vm.PropertyChanged += (snd, e) => propertyChanged.Add(e.PropertyName);

		await UIHelper.Load(ui, ct);
		await TestHelper.WaitFor(() => textBox.Text == "John", _timeout, ct);
		await WaitForInitialSync(propertyChanged, nameof(MvuxValidationViewModel.Person), ct);

		GetErrors(person, "FirstName").Should().BeEmpty("the initial value is valid");

		// Act: invalid input
		textBox.Text = "";
		await TestHelper.WaitFor(() => GetErrors(person, "FirstName").Any(), _timeout, ct);

		GetErrors(person, "FirstName").Should().Equal("First name is required");
		person.HasErrors.Should().BeTrue();
		vm.Person.HasErrors.Should().BeTrue();
		vm.Person.FirstName.Should().Be("", "validation must never block the value pushed by the binding");
		GetErrors(vm, "Person").Should().BeEmpty("the error is about the first name, which is owned by the bindable record");
		vm.HasErrors.Should().BeTrue("the view model aggregates the errors of its sub-bindables");
		vmErrorsChanged.Should().BeEmpty();
		personErrorsChanged.Should().Equal(("FirstName", true));

		// Act: valid input
		textBox.Text = "Jane";
		await TestHelper.WaitFor(() => !GetErrors(person, "FirstName").Any(), _timeout, ct);

		person.HasErrors.Should().BeFalse();
		vm.HasErrors.Should().BeFalse();
		vm.Person.FirstName.Should().Be("Jane");
		personErrorsChanged.Should().Equal(("FirstName", true), ("FirstName", true));
	}

	/// <summary>
	/// Waits for the first value of the state to be pushed to the view by the view model.
	/// </summary>
	/// <remarks>
	/// The bindable reads the current value of the state synchronously when created, but also queues an update on the UI thread for the first message of the state.
	/// If the user edits the value before that queued update runs, the view model (and the view) is reset to the initial value while the state keeps the edited one.
	/// </remarks>
	private static async ValueTask WaitForInitialSync(List<string?> propertyChanged, string propertyName, CancellationToken ct)
		=> await TestHelper.WaitFor(() => propertyChanged.Contains(propertyName), _timeout, ct);

	private static string?[] GetErrors(object bindable, string? propertyName)
		=> ((INotifyDataErrorInfo)bindable).GetErrors(propertyName).Cast<ValidationResult>().Select(result => result.ErrorMessage).ToArray();
}
