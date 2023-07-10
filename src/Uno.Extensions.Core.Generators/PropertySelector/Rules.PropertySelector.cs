using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators;

internal static partial class Rules
{
	public static class PS0001
	{
		private const string message = "'{0}' in '{1}' is not a property member."
			+ " Property selectors can only be of the form `e => e.A.B.C`, you cannot use method nor external value (i.e. cannot have any closure), and cannot be a method group.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0001),
			"A property selector can only use property members",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0001",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(ParenthesizedLambdaExpressionSyntax selectorSyntax, SyntaxNode failingNode)
			=> Diagnostic.Create(
				Descriptor,
				Location.Create(failingNode.SyntaxTree, failingNode.Span),
				failingNode.ToString(),
				selectorSyntax.ToString());
	}

	public static class PS0002
	{
		private const string message = "The variable '{0}' in '{1}' is not the lambda parameter."
			+ " Property selectors can only be of the form `e => e.A.B.C`, you cannot use method nor external value (i.e. cannot have any closure), and cannot be a method group.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0002),
			"A property selector cannot have any closure",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0002",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(ParenthesizedLambdaExpressionSyntax selectorSyntax, SyntaxNode failingNode)
			=> Diagnostic.Create(
				Descriptor,
				Location.Create(failingNode.SyntaxTree, failingNode.Span),
				failingNode.ToString(),
				selectorSyntax.ToString());
	}

	public static class PS0003
	{
		private const string message = "The PropertySelector '{0}' is not a lambda."
			+ " Property selectors can only be of the form `e => e.A.B.C`, you cannot use method nor external value (i.e. cannot have any closure), and cannot be a method group.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0003),
			"A property selector must be a lambda",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0003",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(SyntaxNode selectorSyntax)
			=> Diagnostic.Create(
				Descriptor,
				Location.Create(selectorSyntax.SyntaxTree, selectorSyntax.Span),
				selectorSyntax.ToString());
	}

	public static class PS0004
	{
		private const string message = "The type '{0}' is not a record, it cannot be used as `TEntity` for a `PropertySelector<TEntity, TValue>`";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0004),
			"The `TEntity` of a `PropertySelector<TEntity, TValue>` must be a record",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0004",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(SyntaxNode selectorSyntax, ITypeSymbol type)
			=> Diagnostic.Create(
				Descriptor,
				Location.Create(selectorSyntax.SyntaxTree, selectorSyntax.Span),
				type.ToString());
	}

	public static class PS0005
	{
		private const string message = "In the PropertySelector `{0}`, the type of '{1}' ({2}) is not a record";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0005),
			"All types involved in a PropertySelector must be records",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0005",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(string path, string part, SyntaxNode node, ITypeSymbol type)
			=> Diagnostic.Create(
				Descriptor,
				Location.Create(node.SyntaxTree, node.Span),
				path,
				part,
				type.ToString());
	}

	public static class PS0006
	{
		private const string message = "In the PropertySelector `{0}`, the type of '{1}' ({2}) does not have a constructor which is parameter-less or which accepts only nullable values";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0006),
			"All types involved in a PropertySelector must be constructable without parameter",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0006",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(string path, string part, SyntaxNode node, ITypeSymbol type)
			=> Diagnostic.Create(
				Descriptor,
				Location.Create(node.SyntaxTree, node.Span),
				path,
				part,
				type.ToString());
	}

	public static class PS0007
	{
		private const string message = "Missing '[PropertySelector]' attribute";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0007),
			"Lambda arguments to PropertySelector must have the 'PropertySelectorAttribute'",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0007",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(SyntaxNode node)
			=> Diagnostic.Create(
				Descriptor,
				node.GetLocation());
	}

	public static class PS0101
	{
		private const string message = "The method '{0}' accepts a PropertySelector but is missing either [CallerFilePath] or [CallerLineNumber] parameter. "
			+ "PropertySelector should be used only on public API and converted to `IValueAccessor` before being forwarded to another method.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0101),
			"A method which accepts a PropertySelector must also have 2 parameters flagged with [CallerFilePath] and [CallerLineNumber]",
			message,
			Category.Usage,
			DiagnosticSeverity.Warning,
			helpLinkUri: "https://aka.platform.uno/PS0101",
			isEnabledByDefault: true);

		// TODO: Not yet implemented.
		public static Diagnostic GetDiagnostic(IMethodSymbol method)
			=> Diagnostic.Create(
				Descriptor,
				method.Locations.FirstOrDefault() is { } location
					? location
					: Location.None,
				method.ToString());
	}

	public static class PS0102
	{
		private const string message = "The '{0}' argument used to invoke method '{1}' must a constant value";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS0102),
			"[CallerFilePath] and [CallerLineNumber] arguments used among a PropertySelector argument must be constant values",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS0102",
			isEnabledByDefault: true);

		private static Diagnostic GetDiagnostic(string method, string param, SyntaxNode argNode)
			=> Diagnostic.Create(
				Descriptor,
				Location.Create(argNode.SyntaxTree, argNode.Span),
				param,
				method);

		public static Diagnostic GetFileArgDiagnostic(IMethodSymbol method, IParameterSymbol? param, SyntaxNode argNode)
			=> GetDiagnostic(method.ToString(), "[CallerFilePath] " + (param?.Name ?? ""), argNode);

		public static Diagnostic GetLineArgDiagnostic(IMethodSymbol method, IParameterSymbol? param, SyntaxNode argNode)
			=> GetDiagnostic(method.ToString(), "[CallerLineNumber] " + (param?.Name ?? ""), argNode);
	}

	public static class PS9999
	{
		private const string message = "Code generation of PropertySelector failed: {0}";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(PS9999),
			"Code generation of PropertySelector failed for an unknown reason (see logs for more details)",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://aka.platform.uno/PS9999",
			isEnabledByDefault: true);

		public static Diagnostic GetDiagnostic(Exception error)
			=> Diagnostic.Create(
				Descriptor,
				Location.None,
				error.ToString());
	}
}
