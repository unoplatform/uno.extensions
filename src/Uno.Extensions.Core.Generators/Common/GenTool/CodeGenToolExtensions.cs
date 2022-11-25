﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;

namespace Uno.Extensions.Generators;

internal static class CodeGenToolExtensions
{
	private static SymbolDisplayFormat _symbolDeclaration = SymbolDisplayFormat
		.MinimallyQualifiedFormat
		.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword); // Add `class`, `record` or `record struct`

	public static string GetCodeGenAttribute(this ICodeGenTool tool)
		=> $@"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{tool.GetType().Name}"", ""{tool.Version}"")]";

	public static string GetFileHeader(this ICodeGenTool tool, int aligned = 0)
		=> $@"//----------------------
			// <auto-generated>
			//	Generated by the {tool.GetType().Name} v{tool.Version}. DO NOT EDIT!
			//	Manual changes to this file will be overwritten if the code is regenerated.
			// </auto-generated>
			//----------------------
			#pragma warning disable".Align(Math.Max(aligned - 1, 0));

	public static string AsPartialOf(this ICodeGenTool tool, INamedTypeSymbol type, string code)
		=> AsPartialOf(tool, type, null, null, code);

	public static string AsPartialOf(this ICodeGenTool tool, INamedTypeSymbol type, string? attributes, string? bases, string code)
	{
		var types = type
			.GetContainingTypes()
			.Reverse()
			.Select(t => $"partial {t.ToDisplayString(_symbolDeclaration)}")
			.ToList();
		types.Add($"{attributes?.Align(0)}\r\npartial {type.ToDisplayString(_symbolDeclaration)}{(bases is null ? "" : $" : {bases}")}");

		return $@"{tool.GetFileHeader(3)}

			using global::System;
			using global::System.Linq;
			using global::System.Threading.Tasks;

			namespace {type.ContainingNamespace}
			{{
				{types.Select((def, i) => $"{def.Indent(i + (i == 0 ? 0 : 4))}\r\n{"{".Indent(i + 4)}").JoinBy("\r\n")}
				{code.Align(4 + types.Count)}
				{types.Select((_, i) => "}".Indent((types.Count - 1 - i) + (i == 0 ? 0 : 4))).JoinBy("\r\n")}
			}}".Align(0);
	}

	public static string InSameNamespaceOf(this ICodeGenTool tool, INamedTypeSymbol type, string code)
	{
		var types = type
			.GetContainingTypes()
			.Reverse()
			.Select(t => $"partial {t.ToDisplayString(_symbolDeclaration)}")
			.ToList();

		return $@"{tool.GetFileHeader(3)}

			using global::System;
			using global::System.Linq;
			using global::System.Threading.Tasks;

			namespace {type.ContainingNamespace}
			{{
				{types.Select((def, i) => $"{def.Indent(i + (i == 0 ? 0 : 4))}\r\n{"{".Indent(i + 4)}").JoinBy("\r\n")}
				{code.Align(4 + types.Count)}
				{types.Select((_, i) => "}".Indent((types.Count - 1 - i) + (i == 0 ? 0 : 4))).JoinBy("\r\n")}
			}}".Align(0);
	}
}
