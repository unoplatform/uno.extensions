using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

internal static partial class Rules
{
	public static class KE0001
	{
		private const string message = "The record '{0}' is eligible to IKeyEquatable generation (due to {1}) but is not partial."
			+ " Either make it partial (recommended), either disable implicit IKeyEquality generation using [ImplicitKeys(IsEnabled = false)]"
			+ " on the record itself or on the whole assembly (not recommended).";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(KE0001),
			"Key equatable records must be partial",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/KeyEquality/rules.html#KE0001",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class, params IPropertySymbol[] keys)
			=> string.Format(CultureInfo.InvariantCulture, message, @class.Name, FormatKeys(keys, " and "));

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, ICollection<IPropertySymbol> keys)
			=> Diagnostic.Create(
				Descriptor,
				keys
					.Select(key => key.DeclaringSyntaxReferences.FirstOrDefault())
					.Where(syntax => syntax is not null)
					.Select(syntax => Location.Create(syntax!.SyntaxTree, syntax.Span))
					.FirstOrDefault()
					?? Location.None,
				additionalLocations: keys
					.Select(key => key.DeclaringSyntaxReferences.FirstOrDefault())
					.Where(syntax => syntax is not null)
					.Skip(1)
					.Select(syntax => Location.Create(syntax!.SyntaxTree, syntax.Span)),
				@class.Name,
				FormatKeys(keys, " and "));
	}

	public static class KE0002
	{
		private const string message = "The record '{0}' implements IKeyEquatable.GetKeyHashCode but not IKeyEquatable.KeyEquals";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(KE0002),
			"A record that implements GetKeyHashCode should also implement KeyEquals",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/KeyEquality/rules.html#KE0004",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class)
			=> string.Format(CultureInfo.InvariantCulture, message, @class.Name);

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, IMethodSymbol getKeyHashCode)
			=> Diagnostic.Create(
				Descriptor,
				getKeyHashCode.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				@class.Name);
	}

	public static class KE0003
	{
		private const string message = "The record '{0}' implements IKeyEquatable.KeyEquals but not IKeyEquatable.GetKeyHashCode";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(KE0003),
			"A record that implements KeyEquals should also implement GetKeyHashCode",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/KeyEquality/rules.html#KE0005",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class)
			=> string.Format(CultureInfo.InvariantCulture, message, @class.Name);

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, IMethodSymbol keyEquals)
			=> Diagnostic.Create(
				Descriptor,
				keyEquals.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				@class.Name);
	}

	public static class KE0004
	{
		private const string message = "The record '{0}' is flagged with [ImplicitKeys] attribute, but no property match any of the defined implicit keys."
			+ " The IKeyEquatable implementation cannot be generated."
			+ " You should either remove the [ImplicitKeys] attribute, either add property named {1}.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(KE0004),
			"Records flags with [ImplicitKeys] attribute should have a matching key",
			message,
			Category.Usage,
			DiagnosticSeverity.Warning,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/KeyEquality/rules.html#KE0002",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class, string[] keys)
			=> string.Format(CultureInfo.InvariantCulture, message, @class.Name, FormatKeys(keys, " or "));

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, string[] keys)
			=> Diagnostic.Create(
				Descriptor,
				@class.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				@class.Name,
				FormatKeys(keys, " or "));
	}

	public static class KE0005
	{
		private const string message = "The record '{0}' is eligible for implicit key equality generation, but it has more than one matching key ({1})."
			+ " The IKeyEquatable implementation will use only '{2}'."
			+ " You should either explicitly flag all needed key properties with the [Key] attribute, either remove/rename {3}.";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(KE0005),
			"Key equatable records should have only one implicit key",
			message,
			Category.Usage,
			DiagnosticSeverity.Warning,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/KeyEquality/rules.html#KE0003",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class, ICollection<IPropertySymbol> allPossibleKeys, IPropertySymbol usedKey)
			=> string.Format(
				CultureInfo.InvariantCulture,
				message,
				@class.Name,
				FormatKeys(allPossibleKeys, " and "),
				usedKey.Name,
				FormatKeys(allPossibleKeys.Except(new[] { usedKey }).ToList(), " and "));

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, ICollection<IPropertySymbol> allPossibleKeys, IPropertySymbol usedKey)
			=> Diagnostic.Create(
				Descriptor,
				allPossibleKeys
					.Select(key => key.DeclaringSyntaxReferences.FirstOrDefault())
					.Where(syntax => syntax is not null)
					.Select(syntax => Location.Create(syntax!.SyntaxTree, syntax.Span))
					.FirstOrDefault()
				?? Location.None,
				additionalLocations: allPossibleKeys
					.Select(key => key.DeclaringSyntaxReferences.FirstOrDefault())
					.Where(syntax => syntax is not null)
					.Skip(1)
					.Select(syntax => Location.Create(syntax!.SyntaxTree, syntax.Span)),
				@class.Name,
				FormatKeys(allPossibleKeys, " and "),
				usedKey.Name,
				FormatKeys(allPossibleKeys.Except(new[] { usedKey }).ToList(), " and "));
	}

	public static class KE0006
	{
		private const string message = "The record '{0}' provides custom implemenation of IKeyEquatable but does not implements IKeyed";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(KE0006),
			"A record that implements IKeyEquatable should also implement IKeyed",
			message,
			Category.Usage,
			DiagnosticSeverity.Warning,
			helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/KeyEquality/rules.html#KE0006",
			isEnabledByDefault: true);

		public static string GetMessage(INamedTypeSymbol @class)
			=> string.Format(CultureInfo.InvariantCulture, message, @class.Name);

		public static Diagnostic GetDiagnostic(INamedTypeSymbol @class, IMethodSymbol equatableImpl)
			=> Diagnostic.Create(
				Descriptor,
				equatableImpl.DeclaringSyntaxReferences.FirstOrDefault() is { } syntax
					? Location.Create(syntax.SyntaxTree, syntax.Span)
					: Location.None,
				@class.Name);
	}

	private static string FormatKeys(ICollection<string> keys, string separator)
		=> keys?.Count switch
		{
			<= 0 => throw new InvalidOperationException("You must provide at least one key"),
			1 => $"'{keys.Single()}'",
			_ => $"{keys!.Select(key => $"'{key}'").JoinBy(separator)}",
		};

	private static string FormatKeys(ICollection<IPropertySymbol> keys, string separator)
		=> keys?.Count switch
		{
			<= 0 => throw new InvalidOperationException("You must provide at least one key"),
			1 => $"'{keys.Single().Name}'",
			_ => $"{keys!.Select(key => $"'{key.Name}'").JoinBy(separator)}",
		};
}
