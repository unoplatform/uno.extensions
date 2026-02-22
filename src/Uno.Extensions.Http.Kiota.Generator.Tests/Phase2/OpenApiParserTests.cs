using System.IO;
using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http.Kiota.SourceGenerator.Parsing;

namespace Uno.Extensions.Http.Kiota.Generator.Tests.Phase2;

/// <summary>
/// Unit tests for <see cref="OpenApiDocumentParser"/> covering JSON parsing,
/// YAML parsing, invalid spec error handling, and file extension validation.
/// </summary>
[TestClass]
public class OpenApiParserTests
{
	// ------------------------------------------------------------------
	// Test data paths — resolved relative to the test output directory
	// where MSBuild copies TestData/ at build time.
	// ------------------------------------------------------------------

	private static string TestDataDir =>
		Path.Combine(AppContext.BaseDirectory, "TestData");

	// ------------------------------------------------------------------
	// IsSupportedFileExtension tests
	// ------------------------------------------------------------------

	[TestMethod]
	[DataRow(".json", true)]
	[DataRow(".yaml", true)]
	[DataRow(".yml", true)]
	[DataRow(".JSON", true)]
	[DataRow(".YAML", true)]
	[DataRow(".xml", false)]
	[DataRow(".txt", false)]
	[DataRow("", false)]
	public void IsSupportedFileExtension_ReturnsExpected(string extension, bool expected)
	{
		var path = $"openapi{extension}";
		OpenApiDocumentParser.IsSupportedFileExtension(path).Should().Be(expected);
	}

	[TestMethod]
	public void IsSupportedFileExtension_NullPath_ReturnsFalse()
	{
		OpenApiDocumentParser.IsSupportedFileExtension(null!).Should().BeFalse();
	}

	// ------------------------------------------------------------------
	// JSON parsing
	// ------------------------------------------------------------------

	[TestMethod]
	public void Parse_ValidJsonSpec_ReturnsSuccessResult()
	{
		// Arrange
		var filePath = Path.Combine(TestDataDir, "petstore.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		// Act
		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		// Assert
		result.IsSuccess.Should().BeTrue("the petstore.json spec is a valid OpenAPI document");
		result.Document.Should().NotBeNull();
		result.Document.Paths.Should().NotBeNullOrEmpty("petstore defines at least one path");
	}

	[TestMethod]
	public void Parse_ValidJsonSpec_ExtractsPaths()
	{
		// Arrange
		var filePath = Path.Combine(TestDataDir, "petstore.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		// Act
		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		// Assert
		result.Document.Paths.Should().ContainKey("/pets");
		result.Document.Paths.Should().ContainKey("/pets/{petId}");
	}

	[TestMethod]
	public void Parse_ValidJsonSpec_ExtractsSchemas()
	{
		// Arrange
		var filePath = Path.Combine(TestDataDir, "petstore.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		// Act
		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		// Assert
		result.Document.Components.Should().NotBeNull();
		result.Document.Components.Schemas.Should().ContainKey("Pet");
	}

	// ------------------------------------------------------------------
	// YAML parsing
	// ------------------------------------------------------------------

	[TestMethod]
	public void Parse_ValidYamlSpec_ReturnsSuccessResult()
	{
		// Arrange
		var filePath = Path.Combine(TestDataDir, "petstore.yaml");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		// Act
		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		// Assert
		result.IsSuccess.Should().BeTrue("the petstore.yaml spec is a valid OpenAPI document");
		result.Document.Should().NotBeNull();
		result.Document.Paths.Should().NotBeNullOrEmpty("petstore YAML defines at least one path");
	}

	[TestMethod]
	public void Parse_ValidYamlSpec_ExtractsSchemas()
	{
		// Arrange
		var filePath = Path.Combine(TestDataDir, "petstore.yaml");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		// Act
		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		// Assert
		result.Document.Components.Should().NotBeNull();
		result.Document.Components.Schemas.Should().ContainKey("Pet");
	}

	// ------------------------------------------------------------------
	// Error handling
	// ------------------------------------------------------------------

	[TestMethod]
	public void Parse_NullSourceText_ReturnsFailure()
	{
		var result = OpenApiDocumentParser.Parse(null!, "test.json");

		result.IsSuccess.Should().BeFalse();
		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Should().ContainSingle()
			.Which.Id.Should().Be("KIOTA001");
	}

	[TestMethod]
	public void Parse_EmptySourceText_ReturnsFailure()
	{
		var sourceText = SourceText.From(string.Empty);

		var result = OpenApiDocumentParser.Parse(sourceText, "test.json");

		result.IsSuccess.Should().BeFalse();
		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Should().ContainSingle()
			.Which.Id.Should().Be("KIOTA001");
	}

	[TestMethod]
	public void Parse_WhitespaceOnlySourceText_ReturnsFailure()
	{
		var sourceText = SourceText.From("   \n   \n   ");

		var result = OpenApiDocumentParser.Parse(sourceText, "test.json");

		result.IsSuccess.Should().BeFalse();
		result.HasErrors.Should().BeTrue();
	}

	[TestMethod]
	public void Parse_MalformedJson_ReturnsFailureOrWarnings()
	{
		// The OpenAPI reader may still produce a partial document from
		// invalid JSON. At minimum we expect diagnostics to be reported.
		var sourceText = SourceText.From("{ this is not valid JSON or YAML }");

		var result = OpenApiDocumentParser.Parse(sourceText, "bad.json");

		// Either parsing fails entirely, or the document has errors.
		if (!result.IsSuccess)
		{
			result.HasErrors.Should().BeTrue();
		}
		else
		{
			// OpenAPI reader is lenient — at least it should report a warning/error.
			result.Diagnostics.Should().NotBeEmpty(
				"the reader should report issues with malformed input");
		}
	}

	// ------------------------------------------------------------------
	// Other test specs
	// ------------------------------------------------------------------

	[TestMethod]
	public void Parse_InheritanceSpec_ReturnsSuccess()
	{
		var filePath = Path.Combine(TestDataDir, "inheritance.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		result.IsSuccess.Should().BeTrue();
		result.Document.Should().NotBeNull();
	}

	[TestMethod]
	public void Parse_ComposedTypesSpec_ReturnsSuccess()
	{
		var filePath = Path.Combine(TestDataDir, "composed-types.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		result.IsSuccess.Should().BeTrue();
		result.Document.Should().NotBeNull();
	}

	[TestMethod]
	public void Parse_EnumsSpec_ReturnsSuccess()
	{
		var filePath = Path.Combine(TestDataDir, "enums.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		result.IsSuccess.Should().BeTrue();
		result.Document.Should().NotBeNull();
	}

	[TestMethod]
	public void Parse_ErrorResponsesSpec_ReturnsSuccess()
	{
		var filePath = Path.Combine(TestDataDir, "error-responses.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));

		var result = OpenApiDocumentParser.Parse(sourceText, filePath);

		result.IsSuccess.Should().BeTrue();
		result.Document.Should().NotBeNull();
	}

	// ------------------------------------------------------------------
	// OpenApiParseResult value equality
	// ------------------------------------------------------------------

	[TestMethod]
	public void ParseResult_TwoFailures_WithSameDiagnostic_AreEqual()
	{
		var diag = Microsoft.CodeAnalysis.Diagnostic.Create(
			OpenApiDocumentParser.ParseFailure,
			Microsoft.CodeAnalysis.Location.None,
			"test.json",
			"same error");

		var a = OpenApiParseResult.Failure(diag);
		var b = OpenApiParseResult.Failure(diag);

		// Same diagnostic instance → should be equal.
		a.Should().Be(b);
	}

	[TestMethod]
	public void ParseResult_SuccessWithDocument_IsNotEqualToFailure()
	{
		var filePath = Path.Combine(TestDataDir, "petstore.json");
		var sourceText = SourceText.From(File.ReadAllText(filePath));
		var success = OpenApiDocumentParser.Parse(sourceText, filePath);

		var failure = OpenApiParseResult.Failure(
			Microsoft.CodeAnalysis.Diagnostic.Create(
				OpenApiDocumentParser.ParseFailure,
				Microsoft.CodeAnalysis.Location.None,
				"test.json",
				"error"));

		success.Should().NotBe(failure);
	}
}
