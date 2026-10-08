using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

internal static partial class Rules
{
	// General usage [0000-0999]

	// Bindings [1000-1999]
	public static class FEED1001
	{
		private const string message = "The member '{0}' of '{1}' hides the '{0}' property of the generated bindable '{2}', which exposes the validation state (INotifyDataErrorInfo). "
			+ "Validation errors are still available through the INotifyDataErrorInfo interface, but '{0}' cannot be used to bind to the validation state.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(FEED1001),
			"Member hides the validation state of the generated bindable",
			message,
			Category.Usage,
			DiagnosticSeverity.Info,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Reactive/rules.html#Feed1001",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(INamedTypeSymbol type, ISymbol member, string bindableName)
			=> Diagnostic.Create(
				Descriptor,
				member.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				member.Name,
				type.Name,
				bindableName);
	}


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

	public static class FEED2003
	{
		private const string message = "The validation results of this command cannot be published, as {0}. "
			+ "The execution is still aborted when the parameter is not valid, but the errors are only logged. "
			+ "Use a State as parameter of the command (Given) so the errors are published on it.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(FEED2003),
			"The parameter of a validated command is not a State",
			message,
			Category.Usage,
			DiagnosticSeverity.Warning,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Reactive/rules.html#Feed2003",
			isEnabledByDefault: true);

		public static Diagnostic GetNotAStateDiagnostic(Location location, string parameter, ITypeSymbol type)
			=> Diagnostic.Create(Descriptor, location, $"its parameter '{parameter}' is not a State (it is a '{type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}')");

		public static Diagnostic GetFromViewDiagnostic(Location location)
			=> Diagnostic.Create(Descriptor, location, "its parameter is provided by the view (no Given)");
	}
}
