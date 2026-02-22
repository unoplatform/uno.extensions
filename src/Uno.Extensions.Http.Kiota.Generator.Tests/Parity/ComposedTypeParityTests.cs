using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Parity;

/// <summary>
/// Parity tests that compare source-generator output against Kiota CLI
/// golden files for the <c>composed-types.json</c> OpenAPI spec.
/// <para>
/// The composed-types spec exercises oneOf/anyOf composed type code
/// generation patterns:
/// <list type="bullet">
///   <item>oneOf with object types and discriminator (PaymentMethod)</item>
///   <item>anyOf with multiple object types (NotificationChannels)</item>
///   <item>oneOf with mixed object and primitive types (ContentBlock)</item>
///   <item>oneOf with primitives only (SettingValue)</item>
///   <item>Composed type wrapper classes with IParsable implementation</item>
///   <item>Factory methods for composed types with discriminator</item>
///   <item>Serializer/deserializer dispatch for composed types</item>
///   <item>Request builders with composed type parameters and responses</item>
///   <item>Nested enum types (PaymentResult_status, PushChannel_platform, TextContent_format)</item>
///   <item>Object reference within composed type members (Address)</item>
/// </list>
/// </para>
/// </summary>
[TestClass]
[TestCategory("Parity")]
public class ComposedTypeParityTests : ParityTestBase
{
	// ------------------------------------------------------------------
	// Configuration — composed-types spec
	// ------------------------------------------------------------------

	/// <inheritdoc/>
	protected override string SpecFileName => "composed-types.json";

	/// <inheritdoc/>
	protected override string ClientName => "ComposedTypesClient";

	/// <inheritdoc/>
	protected override string ClientNamespace => "ComposedTypes.Client";

	/// <inheritdoc/>
	protected override string GoldenSubdirectory => "composed-types";

	// ------------------------------------------------------------------
	// Cached results — avoid re-running the generator for each test
	// ------------------------------------------------------------------

	private static GeneratorRunResult? _cachedResult;
	private static Dictionary<string, string>? _cachedGoldenFiles;

	private GeneratorRunResult GetOrRunGenerator()
	{
		if (_cachedResult == null)
		{
			_cachedResult = RunGenerator();
		}
		return _cachedResult;
	}

	private Dictionary<string, string> GetOrLoadGoldenFiles()
	{
		if (_cachedGoldenFiles == null)
		{
			_cachedGoldenFiles = LoadGoldenFiles();
		}
		return _cachedGoldenFiles;
	}

	[ClassCleanup]
	public static void Cleanup()
	{
		_cachedResult = null;
		_cachedGoldenFiles = null;
	}

	// ==================================================================
	// Structural tests — verify the generator produces the expected
	// set of files before comparing content
	// ==================================================================

	[TestMethod]
	public void Generator_ProducesNoErrors()
	{
		var result = GetOrRunGenerator();

		var errors = result.Diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();

		errors.Should().BeEmpty(
			"the generator should not report any errors for the composed-types spec");
	}

	[TestMethod]
	public void Generator_ProducesAtLeastAsManyFilesAsGoldenFiles()
	{
		var result = GetOrRunGenerator();
		var goldenFiles = GetOrLoadGoldenFiles();

		AssertMinimumFileCount(result.GeneratedSources, goldenFiles);
	}

	[TestMethod]
	public void Generator_ProducesAllExpectedFiles()
	{
		var result = GetOrRunGenerator();
		var goldenFiles = GetOrLoadGoldenFiles();

		AssertAllGoldenFilesHaveGeneratedCounterparts(
			result.GeneratedSources, goldenFiles);
	}

	// ==================================================================
	// Full parity — compare every golden file against generated output
	// ==================================================================

	[TestMethod]
	public void AllFiles_MatchGoldenFiles()
	{
		var result = GetOrRunGenerator();
		var goldenFiles = GetOrLoadGoldenFiles();

		AssertParityForAllFiles(result.GeneratedSources, goldenFiles);
	}

	// ==================================================================
	// Individual file parity tests — for targeted debugging when the
	// full parity test fails. Each test isolates one golden file.
	// ==================================================================

	// --- Root client ---

	[TestMethod]
	public void ComposedTypesClient_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("ComposedTypesClient.cs", result.GeneratedSources);
	}

	// --- Request builders ---

	[TestMethod]
	public void Payments_PaymentsRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Payments\\PaymentsRequestBuilder.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Notifications_NotificationsRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Notifications\\NotificationsRequestBuilder.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Content_ContentRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Content\\ContentRequestBuilder.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Settings_SettingsRequestBuilder_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Settings\\SettingsRequestBuilder.cs", result.GeneratedSources);
	}

	// --- Composed type wrapper models (oneOf / anyOf) ---

	[TestMethod]
	public void Models_PaymentMethod_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\PaymentMethod.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_NotificationChannels_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\NotificationChannels.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_ContentBlock_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\ContentBlock.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_SettingValue_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\SettingValue.cs", result.GeneratedSources);
	}

	// --- oneOf member models (PaymentMethod components) ---

	[TestMethod]
	public void Models_CreditCardPayment_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\CreditCardPayment.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_BankTransferPayment_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\BankTransferPayment.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_DigitalWalletPayment_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\DigitalWalletPayment.cs", result.GeneratedSources);
	}

	// --- anyOf member models (NotificationChannels components) ---

	[TestMethod]
	public void Models_EmailChannel_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\EmailChannel.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_SmsChannel_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\SmsChannel.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_PushChannel_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\PushChannel.cs", result.GeneratedSources);
	}

	// --- oneOf mixed-type member models (ContentBlock components) ---

	[TestMethod]
	public void Models_TextContent_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\TextContent.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_ImageContent_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\ImageContent.cs", result.GeneratedSources);
	}

	// --- Regular models ---

	[TestMethod]
	public void Models_Address_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\Address.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_ApiError_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\ApiError.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_PaymentResult_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\PaymentResult.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_NotificationRequest_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\NotificationRequest.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_NotificationResult_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\NotificationResult.cs", result.GeneratedSources);
	}

	// --- Enum models ---

	[TestMethod]
	public void Models_PaymentResult_status_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\PaymentResult_status.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_PushChannel_platform_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\PushChannel_platform.cs", result.GeneratedSources);
	}

	[TestMethod]
	public void Models_TextContent_format_MatchesGoldenFile()
	{
		var result = GetOrRunGenerator();
		AssertParityForFile("Models\\TextContent_format.cs", result.GeneratedSources);
	}
}
