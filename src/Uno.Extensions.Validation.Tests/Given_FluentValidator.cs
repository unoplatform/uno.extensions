using FluentValidation;
using FluentValidation.Results;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Uno.Extensions.Validation.Tests;

[TestClass]
public class Given_FluentValidator
{
	[TestMethod]
	public async Task When_PropertyInvalid_Then_MemberNamesContainsPropertyName()
	{
		using var host = CreateHost(b => b.Validator<Person, PersonValidator>());

		var results = await host.GetValidator().ValidateAsync(new Person(""));

		results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(Person.FirstName));
	}

	[TestMethod]
	public async Task When_ObjectLevelFailure_Then_MemberNamesEmpty()
	{
		using var host = CreateHost(b => b.Validator<Person, ObjectLevelValidator>());

		var results = await host.GetValidator().ValidateAsync(new Person("x"));

		var result = results.Should().ContainSingle().Subject;
		result.ErrorMessage.Should().Be("object error");
		result.MemberNames.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_WithMessageKey_Then_KeyPassedAsIs()
	{
		using var host = CreateHost(b => b.Validator<Person, PersonValidator>());

		var results = await host.GetValidator().ValidateAsync(new Person(""));

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Validation_FirstNameRequired");
	}

	private static IHost CreateHost(Func<IValidationBuilder, IHostBuilder> configure)
		=> TestHost.Create(configure);

	public sealed record Person(string FirstName);

	public sealed class PersonValidator : AbstractValidator<Person>
	{
		public PersonValidator()
		{
			RuleFor(p => p.FirstName)
				.NotEmpty()
				.WithMessage("Validation_FirstNameRequired");
		}
	}

	public sealed class ObjectLevelValidator : AbstractValidator<Person>
	{
		protected override bool PreValidate(ValidationContext<Person> context, FluentValidation.Results.ValidationResult result)
		{
			result.Errors.Add(new ValidationFailure(string.Empty, "object error"));
			return false;
		}
	}
}
