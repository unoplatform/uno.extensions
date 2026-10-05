namespace Uno.Extensions.Validation.Tests;

[TestClass]
public class Given_LocalizedDataAnnotations
{
	[TestMethod]
	public async Task When_KeyFound_Then_MessageLocalized()
	{
		var results = await Validate(new Person(), new()
		{
			["Validation_NameRequired"] = "{0} est requis",
			["Person_Name"] = "Nom",
		});

		var result = results.Should().ContainSingle().Subject;
		result.ErrorMessage.Should().Be("Nom est requis");
		result.MemberNames.Should().Equal(nameof(Person.Name));
	}

	[TestMethod]
	public async Task When_KeyMissing_Then_OriginalMessage()
	{
		var results = await Validate(new Person(), new() { ["Person_Name"] = "Nom" });

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Validation_NameRequired");
	}

	[TestMethod]
	public async Task When_NoMessageKey_Then_DefaultMessageWithLocalizedDisplayName()
	{
		var results = await Validate(new WithDefaultMessage(), new() { ["Person_Name"] = "Nom" });

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("The Nom field is required.");
	}

	[TestMethod]
	public async Task When_DefaultMessageKeyFormat_Then_ConventionKeyUsed()
	{
		var results = await Validate(
			new WithDefaultMessage(),
			new() { ["Validation_Required"] = "{0} requis", ["Person_Name"] = "Nom" },
			o => o.DefaultMessageKeyFormat = "Validation_{0}");

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Nom requis");
	}

	[TestMethod]
	public async Task When_ErrorMessageResourceType_Then_NotLocalized()
	{
		var results = await Validate(new WithResourceType(), new() { [nameof(StaticResources.Required)] = "localized" });

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("static Value");
	}

	[TestMethod]
	public async Task When_CustomMessageFromIsValid_Then_NotLocalized()
	{
		var results = await Validate(new WithCustomMessage(), new() { ["Validation_Custom"] = "localized" });

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("custom message");
	}

	[TestMethod]
	public async Task When_InvalidTranslation_Then_OriginalMessage()
	{
		var results = await Validate(new Person(), new() { ["Validation_NameRequired"] = "{0} {3}" });

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Validation_NameRequired");
	}

	[TestMethod]
	public async Task When_FormatArguments_Then_SameAsAttributes()
	{
		var results = await Validate(
			new WithArguments(),
			new()
			{
				["StringLength"] = "{0}|{1}|{2}",
				["Range"] = "{0}|{1}|{2}",
				["MinLength"] = "{0}|{1}",
				["MaxLength"] = "{0}|{1}",
				["Length"] = "{0}|{1}|{2}",
				["Compare"] = "{0}|{1}",
				["Regex"] = "{0}|{1}",
				["Other_Name"] = "Autre",
			});

		results.Select(r => r.ErrorMessage).Should().BeEquivalentTo(
			"StringLength|5|2",
			"Range|1|9",
			"MinLength|3",
			"MaxLength|1",
			"Length|1|2",
			"Compare|Autre",
			"Regex|^[a-z]+$");
	}

	[TestMethod]
	public async Task When_Formatting_Then_CurrentCultureUsed()
	{
		var culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
		try
		{
			var results = await Validate(new WithDecimalRange(), new() { ["Range"] = "{1}" });

			results.Should().ContainSingle().Which.ErrorMessage.Should().Be("1,5");
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[TestMethod]
	public async Task When_LocalizerChangesBetweenValidations_Then_NotCached()
	{
		var localizer = new FakeStringLocalizer(new() { ["Validation_NameRequired"] = "first" });
		using var host = CreateHost(localizer);
		var validator = host.GetValidator();

		(await validator.ValidateAsync(new Person())).Single().ErrorMessage.Should().Be("first");
		localizer.Resources["Validation_NameRequired"] = "second";
		(await validator.ValidateAsync(new Person())).Single().ErrorMessage.Should().Be("second");
	}

	[TestMethod]
	public async Task When_ClassLevelAttribute_Then_LocalizedWithTypeDisplayName()
	{
		var results = await Validate(new WithClassLevel(), new() { ["Validation_Class"] = "{0} invalide", ["Model_Name"] = "Modèle" });

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Modèle invalide");
	}

	[TestMethod]
	public async Task When_NoKeyFound_Then_SameResultsAsBcl()
	{
		object[] instances =
		[
			new ParityModel { Required = null, Short = "too long", Other = "a", Confirm = "b" },
			new ParityModel { Required = "ok", Short = "ok", Other = "a", Confirm = "a" },
			new ValidatableModel { Value = "ok" },
			new ValidatableModel { Value = null },
			new ParityNestedModel { Nested = new() },
		];

		foreach (var instance in instances)
		{
			var results = await Validate(instance, new());

			results.Should().BeEquivalentTo(BclValidation.Validate(instance), opts => opts.WithStrictOrdering(), instance.ToString());
		}
	}

	private static async Task<IReadOnlyList<ValidationResult>> Validate(object instance, Dictionary<string, string> resources, Action<DataAnnotationsLocalizationOptions>? configure = null)
	{
		using var host = CreateHost(new FakeStringLocalizer(resources), configure);
		return (await host.GetValidator().ValidateAsync(instance)).ToList();
	}

	private static IHost CreateHost(IStringLocalizer localizer, Action<DataAnnotationsLocalizationOptions>? configure = null)
		=> TestHost.Create(b => b.UseLocalizedDataAnnotations(configure), s => s.AddSingleton(localizer));

	public sealed class Person
	{
		[Display(Name = "Person_Name")]
		[Required(ErrorMessage = "Validation_NameRequired")]
		public string? Name { get; set; }
	}

	public sealed class WithDefaultMessage
	{
		[Display(Name = "Person_Name")]
		[Required]
		public string? Name { get; set; }
	}

	public static class StaticResources
	{
		public static string Required => "static {0}";
	}

	public sealed class WithResourceType
	{
		[Required(ErrorMessageResourceType = typeof(StaticResources), ErrorMessageResourceName = nameof(StaticResources.Required))]
		public string? Value { get; set; }
	}

	private sealed class CustomMessageAttribute : ValidationAttribute
	{
		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
			=> new("custom message", [validationContext.MemberName!]);
	}

	public sealed class WithCustomMessage
	{
		[CustomMessage(ErrorMessage = "Validation_Custom")]
		public string? Value { get; set; }
	}

	public sealed class WithArguments
	{
		[StringLength(5, MinimumLength = 2, ErrorMessage = "StringLength")]
		public string StringLength { get; set; } = "x";

		[Range(1, 9, ErrorMessage = "Range")]
		public int Range { get; set; } = 10;

		[MinLength(3, ErrorMessage = "MinLength")]
		public string MinLength { get; set; } = "x";

		[MaxLength(1, ErrorMessage = "MaxLength")]
		public string MaxLength { get; set; } = "xx";

		[Length(1, 2, ErrorMessage = "Length")]
		public string Length { get; set; } = "xxx";

		[Display(Name = "Other_Name")]
		public string Other { get; set; } = "a";

		[Compare(nameof(Other), ErrorMessage = "Compare")]
		public string Compare { get; set; } = "b";

		[RegularExpression("^[a-z]+$", ErrorMessage = "Regex")]
		public string Regex { get; set; } = "1";
	}

	public sealed class WithDecimalRange
	{
		[Range(1.5, 9.5, ErrorMessage = "Range")]
		public double Value { get; set; } = 10;
	}

	[Display(Name = "Model_Name")]
	[AlwaysInvalid(ErrorMessage = "Validation_Class")]
	public sealed class WithClassLevel
	{
	}

	[AttributeUsage(AttributeTargets.Class)]
	private sealed class AlwaysInvalidAttribute : ValidationAttribute
	{
		public override bool IsValid(object? value) => false;
	}

	public sealed class ValidatableModel : IValidatableObject
	{
		[Required]
		public string? Value { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			yield return ValidationResult.Success!;
			yield return new("validatable", [nameof(Value)]);
		}

		public override string ToString() => $"{nameof(ValidatableModel)}({Value})";
	}
}
