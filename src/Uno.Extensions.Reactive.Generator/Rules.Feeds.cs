using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

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
}
