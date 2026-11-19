namespace Uno.Extensions.Validation.Tests;

/// <summary>
/// The validation layer does not localize: a resource key used as message is reported as-is (localization is done by MVUX State.Validate).
/// </summary>
[TestClass]
public class Given_Validator
{
	[TestMethod]
	public async Task When_ErrorMessageIsKey_Then_KeyPassedAsIs()
	{
		using var host = TestHost.Create();

		var results = await host.GetValidator().ValidateAsync(new RequiredModel());

		var result = results.Should().ContainSingle().Subject;
		result.ErrorMessage.Should().Be("Validation_Required");
		result.MemberNames.Should().Equal(nameof(RequiredModel.Value));
	}

	[TestMethod]
	public async Task When_ErrorMessageIsKeyOfAttributeWithArguments_Then_KeyPassedAsIs()
	{
		using var host = TestHost.Create();

		var results = await host.GetValidator().ValidateAsync(new ArgumentsModel { Text = "too long", Number = 42 });

		results.Select(result => result.ErrorMessage).Should().BeEquivalentTo("Validation_Length", "Validation_Range");
	}

	private sealed class RequiredModel
	{
		[Display(Name = "Model_Value")]
		[Required(ErrorMessage = "Validation_Required")]
		public string? Value { get; set; }
	}

	private sealed class ArgumentsModel
	{
		[StringLength(3, MinimumLength = 1, ErrorMessage = "Validation_Length")]
		public string? Text { get; set; }

		[Range(0, 10, ErrorMessage = "Validation_Range")]
		public int Number { get; set; }
	}
}
