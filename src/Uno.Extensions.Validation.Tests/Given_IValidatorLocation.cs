namespace Uno.Extensions.Validation.Tests;

/// <summary>
/// The IValidator is declared in Uno.Extensions.Core (so MVUX can use it without depending on this package),
/// and forwarded from Uno.Extensions.Validation for binaries compiled against its previous location.
/// </summary>
[TestClass]
public class Given_IValidatorLocation
{
	[TestMethod]
	public void When_GetAssembly_Then_IsCore()
		=> typeof(IValidator).Assembly.GetName().Name.Should().Be("Uno.Extensions.Core");

	[TestMethod]
	public void When_ResolvedFromValidationAssembly_Then_ForwardedToCore()
		=> Type.GetType("Uno.Extensions.Validation.IValidator, Uno.Extensions.Validation", throwOnError: true)
			.Should().BeSameAs(typeof(IValidator));

	[TestMethod]
	public async Task When_ResolvedFromServices_Then_Validates()
	{
		using var host = TestHost.Create();

		// The IValidator of Core is the one registered by UseValidation.
		var results = await host.Services.GetRequiredService<IValidator>().ValidateAsync(new Model());

		var result = results.Should().ContainSingle().Subject;
		result.ErrorMessage.Should().Be("required");
		result.MemberNames.Should().Equal(nameof(Model.Value));
	}

	private sealed class Model
	{
		[Required(ErrorMessage = "required")]
		public string? Value { get; set; }
	}
}
