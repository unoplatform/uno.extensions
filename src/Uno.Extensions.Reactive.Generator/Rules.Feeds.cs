using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

internal static partial class Rules
{
	// General usage [0000-0999]

	// Bindings [1000-1999]

	// Commands [2000-2999]
	public static class FEED2001
	{
		private const string message = "There is no public property '{0}' on the class '{1}' that can be used as command parameter for '{2}'";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(FEED2001),
			"Invalid feed property name",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Reactive/rules.html#Feed2001",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class, IMethodSymbol method, string missingPropertyName)
			=> string.Format(CultureInfo.InvariantCulture, message, missingPropertyName, method.Name, @class.Name);

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, IMethodSymbol method, IParameterSymbol parameter)
			=> Diagnostic.Create(
				Descriptor,
				parameter.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				parameter.Name,
				@class.Name,
				method.Name);
	}

	public static class FEED2002
	{
		private const string message = "The property '{0}' resolved on the class '{1}' is not a Feed. It cannot be used as command parameter for '{2}'.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(FEED2002),
			"Invalid property type",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Reactive/rules.html#Feed2002",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class, IMethodSymbol method, string missingPropertyName)
			=> string.Format(CultureInfo.InvariantCulture, message, missingPropertyName, method.Name, @class.Name);

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, IMethodSymbol method, IParameterSymbol parameter)
			=> Diagnostic.Create(
				Descriptor,
				parameter.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				parameter.Name,
				@class.Name,
				method.Name);
	}

	// Mocks [3000-3999]
	public static class FEED3001
	{
		private const string message = "Mock generation is enabled for the model '{0}', but its base model '{1}' is not mock-enabled (its assembly must also match a GenerateModelMocks pattern). No mock factory will be generated for '{0}'.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(FEED3001),
			"Mock generation skipped",
			message,
			Category.Usage,
			DiagnosticSeverity.Warning,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Reactive/rules.html#Feed3001",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(INamedTypeSymbol model, INamedTypeSymbol baseModel)
			=> Diagnostic.Create(
				Descriptor,
				model.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				model.Name,
				baseModel.Name);
	}

	public static class FEED3002
	{
		private const string message = "The GenerateModelMocks pattern '{0}' does not match any generated bindable view model of this assembly";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(FEED3002),
			"Unmatched mock generation pattern",
			message,
			Category.Usage,
			DiagnosticSeverity.Warning,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Reactive/rules.html#Feed3002",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(string pattern, Location location)
			=> Diagnostic.Create(Descriptor, location, pattern);
	}
}
