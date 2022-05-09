using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record CommandFromMethod(IMethodSymbol Method, BindableGenerationContext Context) : IMappedMember
{
	/// <inheritdoc />
	public string Name => Method.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{Method.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IAsyncCommand {Name} {{ get; }}";

	/// <inheritdoc />
	public string? GetInitialization()
		=> @$"{Name} = new {NS.Reactive}.AsyncCommand(
			nameof({Name}),
			new {NS.Reactive}.CommandConfig
			{{
				Execute = async (commandParameter, ct) => {GetAwait()}{N.Ctor.Model}.{Method.Name}({GetArguments()})
			}},
			{NS.Reactive}.Command.DefaultErrorHandler,
			{N.Ctor.Ctx});";


	public static bool IsSupported(IMethodSymbol method, BindableGenerationContext context)
		=> method is
			{
				MethodKind: MethodKind.Ordinary,
				IsImplicitlyDeclared: false,
				DeclaredAccessibility: Accessibility.Public,
				Parameters.Length: 0 or 1 or 2
			}
			&& method.Parameters.Count(parameter => !SymbolEqualityComparer.Default.Equals(parameter.Type, context.CancellationToken)) <= 1;

	private string GetArguments()
		=> Method.Parameters.Length switch
		{
			0 => "",
			1 when SymbolEqualityComparer.Default.Equals(Method.Parameters[0].Type, Context.CancellationToken) => "ct",
			1 => $"({Method.Parameters[0].Type}) commandParameter",
			2 when SymbolEqualityComparer.Default.Equals(Method.Parameters[0].Type, Context.CancellationToken) => $"ct, ({Method.Parameters[0].Type}) commandParameter",
			2 => $"({Method.Parameters[0].Type}) commandParameter, ct",
			_ => throw new InvalidOperationException("Invalid number of arguments to create a command.")
		};

	private string GetAwait()
		=> Context.IsAwaitable(Method)
			? "await "
			: string.Empty;
}
