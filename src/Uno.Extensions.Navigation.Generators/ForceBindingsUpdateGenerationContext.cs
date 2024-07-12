using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;
using Uno.Extensions.Navigation.UI.Controls;

namespace Uno.Extensions.Navigation.Generators;

internal record ForceBindingsUpdateGenerationContext(
	GeneratorExecutionContext Context,

	// Core types
	[ContextType("Uno.Extensions.Navigation.IForceBindingsUpdate")] INamedTypeSymbol ForceBindingsUpdateInterface,

	// Attributes
	[ContextType(typeof(ForceUpdateAttribute))] INamedTypeSymbol UpdateAttribute,

	// General stuff types
	[ContextType("Microsoft.UI.Xaml.Controls.Page")] INamedTypeSymbol Page)
{
	private IImmutableSet<string>? _XBindFiles;
	private static readonly Regex ClassRegEx = new Regex("x:Class=\"([\\w.]+)\"");
	private const string xBind = "{x:Bind ";

	public bool IsGenerationNotDisable(ISymbol symbol)
		=> IsGenerationEnabled(symbol) ?? true;

	public bool? IsGenerationEnabled(ISymbol symbol)
		=> symbol.FindAttributeValue<bool>(UpdateAttribute, nameof(ForceUpdateAttribute.IsEnabled), 0) is { isDefined: true } attribute
			? attribute.value ?? true
			: null;

	private string? ExtractXBindClassName(AdditionalText file)
	{
		if (Context.CancellationToken.IsCancellationRequested)
		{
			return default;
		}

		string? className = null;
		var hasXBind = false;

		using var reader = new StreamReader(file.Path);

		while (
			!reader.EndOfStream &&
			(className is null ||
			!hasXBind))
		{
			if (Context.CancellationToken.IsCancellationRequested)
			{
				return default;
			}

			var txt = reader.ReadLine();
			if (className is null)
			{
				var classNameMatch = ClassRegEx.Match(txt);
				if (classNameMatch.Success &&
					classNameMatch.Groups.Count > 1)
				{
					className = classNameMatch.Groups[1].Value;
				}
			}

			if (txt is not null &&
				txt.Contains(xBind))
			{
				hasXBind = true;
			}
		}

		return hasXBind ? className : default;
	}

	private IImmutableSet<string> XBindFiles
	{
		get
		{
			_XBindFiles ??= Context
								.AdditionalFiles
								.Select(ExtractXBindClassName)
								.Where(className => className is not null)
								.Select(x => x!) // Is there a better way to force not null here?
								.ToImmutableHashSet();
			return _XBindFiles;
		}
	}

	public bool ContainsXBind(INamedTypeSymbol symbol)
	{
		var name = symbol.ToDisplayString();
		return XBindFiles.Contains(name);
	}
}
