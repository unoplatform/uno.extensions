namespace Uno.Extensions.Validation.Tests;

[TestClass]
public class Given_Validator
{
	[TestMethod]
	public async Task When_PropertyAttributeUsesServices_Then_AppServicesAvailable()
	{
		using var host = TestHost.Create(services: s => s.AddSingleton(new Marker("app")));

		var results = await host.GetValidator().ValidateAsync(new WithServiceAwareProperty());

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("app");
	}

	[TestMethod]
	public async Task When_ValidatableObjectUsesServices_Then_AppServicesAvailable()
	{
		using var host = TestHost.Create(services: s => s.AddSingleton(new Marker("app")));

		var results = await host.GetValidator().ValidateAsync(new ServiceAwareValidatable());

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("app");
	}

	[TestMethod]
	public async Task When_ContextProvided_Then_ContextUsed()
	{
		using var host = TestHost.Create(services: s => s.AddSingleton(new Marker("app")));
		var instance = new ServiceAwareValidatable();
		var context = new ValidationContext(instance, new SingleServiceProvider(new Marker("caller")), items: null);

		var results = await host.GetValidator().ValidateAsync(instance, context);

		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("caller");
	}

	[TestMethod]
	public async Task When_NotLocalized_Then_SameResultsAsBcl()
	{
		using var host = TestHost.Create();
		var instance = new ParityModel { Required = null, Short = "too long", Other = "a", Confirm = "b" };

		var results = await host.GetValidator().ValidateAsync(instance);

		results.Should().BeEquivalentTo(BclValidation.Validate(instance), opts => opts.WithStrictOrdering());
	}

	internal sealed record Marker(string Name);

	private sealed class SingleServiceProvider(object service) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
	}

	private sealed class ServiceAwareAttribute : ValidationAttribute
	{
		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
			=> new((validationContext.GetService(typeof(Marker)) as Marker)?.Name ?? "no service");
	}

	private sealed class WithServiceAwareProperty
	{
		[ServiceAware]
		public string? Value { get; set; }
	}

	private sealed class ServiceAwareValidatable : IValidatableObject
	{
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			yield return new((validationContext.GetService(typeof(Marker)) as Marker)?.Name ?? "no service");
		}
	}
}
