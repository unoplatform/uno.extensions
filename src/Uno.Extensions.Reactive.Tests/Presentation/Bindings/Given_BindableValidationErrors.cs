using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;

namespace Uno.Extensions.Reactive.Tests.Presentation.Bindings;

[TestClass]
public class Given_BindableValidationErrors
{
	[TestMethod]
	public void When_ResultsReceivedBeforeSubBindableSubscribed_Then_ReRouted()
	{
		var changed = new List<string>();
		var hasErrorsChanged = 0;
		var sut = new BindableValidationErrors(changed.Add, () => hasErrorsChanged++);
		var street = new ValidationResult("Street is required", new[] { "Address.Street" });

		sut.Update(ImmutableList.Create(new BindableValidationResult(street, "Address.Street")));

		// No sub-bindable yet, the owner is the one which exposes the error.
		sut.GetErrors("Address").Cast<ValidationResult>().Should().Equal(street);
		changed.Should().Equal("Address");
		hasErrorsChanged.Should().Be(1);

		var forwarded = new List<IImmutableList<BindableValidationResult>>();
		sut.Subscribe("Address", forwarded.Add);

		forwarded.Should().ContainSingle().Which.Should().Equal(new BindableValidationResult(street, "Street"));
		sut.GetErrors("Address").Cast<ValidationResult>().Should().BeEmpty();
		changed.Should().Equal("Address", "Address");
		sut.HasErrors.Should().BeTrue("the sub-bindable has errors");
		hasErrorsChanged.Should().Be(1);
	}

	[TestMethod]
	public void When_PropertyResultsReceivedBeforeSubBindableSubscribed_Then_ReRouted()
	{
		var sut = new BindableValidationErrors(_ => { }, () => { });
		var firstName = new ValidationResult("First name is required", new[] { "FirstName" });

		sut.UpdateProperty("Person", ImmutableList.Create(firstName));
		sut.GetErrors("Person").Cast<ValidationResult>().Should().Equal(firstName);

		var forwarded = new List<IImmutableList<BindableValidationResult>>();
		sut.Subscribe("Person", forwarded.Add);

		forwarded.Should().ContainSingle().Which.Should().Equal(new BindableValidationResult(firstName, "FirstName"));
		sut.GetErrors("Person").Cast<ValidationResult>().Should().BeEmpty();
	}

	[TestMethod]
	public void When_ResultTargetsSameMemberMultipleTimes_Then_Deduplicated()
	{
		var sut = new BindableValidationErrors(_ => { }, () => { });
		var result = new ValidationResult("Invalid", new[] { "Name", "Name" });

		sut.UpdateProperty("Name", ImmutableList.Create(result));

		sut.GetErrors("Name").Cast<ValidationResult>().Should().Equal(result);
	}

	[TestMethod]
	public void When_UpdateProperty_Then_OtherPropertiesKept()
	{
		var changed = new List<string>();
		var sut = new BindableValidationErrors(changed.Add, () => { });
		var name = new ValidationResult("Name");
		var age = new ValidationResult("Age");

		sut.UpdateProperty("Name", ImmutableList.Create(name));
		sut.UpdateProperty("Age", ImmutableList.Create(age));
		sut.UpdateProperty("Name", ImmutableList<ValidationResult>.Empty);

		sut.GetErrors("Name").Cast<ValidationResult>().Should().BeEmpty();
		sut.GetErrors("Age").Cast<ValidationResult>().Should().Equal(age);
		sut.HasErrors.Should().BeTrue();
		changed.Should().Equal("Name", "Age", "Name");
	}
}
