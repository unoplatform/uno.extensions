namespace Uno.Extensions.Validation.Tests;

[TestClass]
public class Given_UseLocalizedDataAnnotations
{
	private static readonly FakeStringLocalizer _localizer = new(new() { ["Validation_Required"] = "localized" });

	[TestMethod]
	public async Task When_NotUsed_Then_NotLocalized()
	{
		using var host = TestHost.Create(services: s => s.AddSingleton<IStringLocalizer>(_localizer));

		var results = await host.GetValidator().ValidateAsync(new Model());

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Validation_Required");
	}

	[TestMethod]
	public async Task When_UsedWithoutLocalizer_Then_NotLocalized()
	{
		using var host = TestHost.Create(b => b.UseLocalizedDataAnnotations());

		var results = await host.GetValidator().ValidateAsync(new Model());

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Validation_Required");
	}

	[TestMethod]
	public async Task When_Used_Then_Localized()
	{
		using var host = TestHost.Create(b => b.UseLocalizedDataAnnotations(), s => s.AddSingleton<IStringLocalizer>(_localizer));

		var results = await host.GetValidator().ValidateAsync(new Model());

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("localized");
	}

	[TestMethod]
	public async Task When_LocalizerProviderConfigured_Then_UsedWithModelType()
	{
		var requestedTypes = new List<Type>();
		using var host = TestHost.Create(b => b.UseLocalizedDataAnnotations(o => o.LocalizerProvider = (type, _) =>
		{
			requestedTypes.Add(type);
			return _localizer;
		}));

		var results = await host.GetValidator().ValidateAsync(new Model());

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("localized");
		requestedTypes.Should().Equal(typeof(Model));
	}

	[TestMethod]
	public async Task When_LocalizerProviderReturnsNull_Then_NotLocalized()
	{
		using var host = TestHost.Create(
			b => b.UseLocalizedDataAnnotations(o => o.LocalizerProvider = (_, _) => null),
			s => s.AddSingleton<IStringLocalizer>(_localizer));

		var results = await host.GetValidator().ValidateAsync(new Model());

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("Validation_Required");
	}

	[TestMethod]
	public void When_Used_Then_OptionsBound()
	{
		using var host = TestHost.Create(b => b.UseLocalizedDataAnnotations(o => o.DefaultMessageKeyFormat = "Key_{0}"));

		host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataAnnotationsLocalizationOptions>>()
			.Value.DefaultMessageKeyFormat.Should().Be("Key_{0}");
	}

	[TestMethod]
	public async Task When_ChainedWithFluentValidator_Then_BothApplied()
	{
		using var host = TestHost.Create(
			b => b.UseLocalizedDataAnnotations().Validator<Given_FluentValidator.Person, Given_FluentValidator.PersonValidator>(),
			s => s.AddSingleton<IStringLocalizer>(new FakeStringLocalizer(new()
			{
				["Validation_Required"] = "localized",
				["Validation_FirstNameRequired"] = "fluent",
			})));
		var validator = host.GetValidator();

		(await validator.ValidateAsync(new Model())).Should().ContainSingle().Which.ErrorMessage.Should().Be("localized");
		(await validator.ValidateAsync(new Given_FluentValidator.Person(""))).Should().ContainSingle().Which.ErrorMessage.Should().Be("fluent");
	}

	public sealed class Model
	{
		[Required(ErrorMessage = "Validation_Required")]
		public string? Value { get; set; }
	}
}
