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
	public void When_ResolvedFromServices_Then_ImplementedByValidationPackage()
	{
		using var host = TestHost.Create();

		var validator = host.GetValidator();

		validator.GetType().Assembly.GetName().Name.Should().Be("Uno.Extensions.Validation");
	}
}
