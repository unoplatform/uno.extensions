using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator.Commands;

/// <summary>
/// Reports the validation of a command (CommandBuilderExtensions.Validation) whose parameter is not a state (FEED2003),
/// as the validation results cannot be published (they are only logged).
/// </summary>
/// <remarks>
/// This follows the fluent configuration of the command (`.Given(x).When(...).Validation(...)`) back to its parameter.
/// Configurations that cannot be followed (e.g. a builder stored in a local) are not reported, to avoid false positives.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandValidationAnalyzer : DiagnosticAnalyzer
{
	private sealed record Symbols(
		INamedTypeSymbol Extensions,
		INamedTypeSymbol Command,
		INamedTypeSymbol Builder,
		INamedTypeSymbol TypedBuilder,
		INamedTypeSymbol State);

	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rules.FEED2003.Descriptor);

	/// <inheritdoc />
	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterCompilationStartAction(static context =>
		{
			var compilation = context.Compilation;
			if (compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.CommandBuilderExtensions") is { } extensions
				&& compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.Command") is { } command
				&& compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.ICommandBuilder") is { } builder
				&& compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.ICommandBuilder`1") is { } typedBuilder
				&& compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.IState`1") is { } state)
			{
				var symbols = new Symbols(extensions, command, builder, typedBuilder, state);
				context.RegisterOperationAction(context => AnalyzeInvocation(context, symbols), OperationKind.Invocation);
			}
		});
	}

	private static void AnalyzeInvocation(OperationAnalysisContext context, Symbols symbols)
	{
		var validation = (IInvocationOperation)context.Operation;
		if (validation.TargetMethod is not { Name: "Validation", IsExtensionMethod: true } method
			|| !SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Extensions)
			|| validation.Arguments.FirstOrDefault(arg => arg.Parameter?.Ordinal == 0) is not { } builderArg)
		{
			return;
		}

		var receiver = Unwrap(builderArg.Value);
		while (true)
		{
			switch (receiver)
			{
				case IInvocationOperation { TargetMethod.Name: "When" } whenInvocation
					when IsMemberOf(whenInvocation.TargetMethod, symbols.TypedBuilder):
					receiver = Unwrap(whenInvocation.Instance);
					break;

				case IInvocationOperation { TargetMethod.Name: "Given", Arguments.Length: 1 } given
					when SymbolEqualityComparer.Default.Equals(given.TargetMethod.ContainingType, symbols.Builder):
					// Note: We also unwrap explicit conversions (e.g. `Given((IFeed<T>)MyState)`), as only the runtime type matters to publish the results.
					var parameter = Unwrap(given.Arguments[0].Value, includeExplicit: true);
					if (parameter?.Type is { } type and not IErrorTypeSymbol && !IsState(type, symbols.State))
					{
						context.ReportDiagnostic(Rules.FEED2003.GetNotAStateDiagnostic(GetLocation(validation), parameter.Syntax.ToString(), type));
					}
					return;

				case IParameterReferenceOperation { Parameter: var builderParameter }
					when IsBuildDelegateOfCommandCreate(validation, builderParameter, symbols):
					context.ReportDiagnostic(Rules.FEED2003.GetFromViewDiagnostic(GetLocation(validation)));
					return;

				default:
					return; // Unknown configuration, we cannot determine the parameter of the command.
			}
		}
	}

	private static IOperation? Unwrap(IOperation? operation, bool includeExplicit = false)
	{
		while (operation is IConversionOperation conversion && (includeExplicit || conversion.IsImplicit))
		{
			operation = conversion.Operand;
		}

		return operation;
	}

	private static bool IsMemberOf(IMethodSymbol method, INamedTypeSymbol type)
		=> SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, type);

	private static bool IsState(ITypeSymbol type, INamedTypeSymbol state)
		=> SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, state)
			|| type.AllInterfaces.Any(@interface => SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, state));

	/// <summary>
	/// Determines if the builder is the parameter of the lambda given to Command.Create, i.e. the parameter of the command is provided by the view.
	/// </summary>
	private static bool IsBuildDelegateOfCommandCreate(IOperation operation, IParameterSymbol builderParameter, Symbols symbols)
	{
		for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
		{
			if (parent is IAnonymousFunctionOperation lambda
				&& SymbolEqualityComparer.Default.Equals(lambda.Symbol, builderParameter.ContainingSymbol))
			{
				return lambda.Parent is IDelegateCreationOperation { Parent: IArgumentOperation { Parent: IInvocationOperation create } }
					&& create.TargetMethod.Name == "Create"
					&& SymbolEqualityComparer.Default.Equals(create.TargetMethod.ContainingType, symbols.Command);
			}
		}

		return false;
	}

	private static Location GetLocation(IInvocationOperation validation)
		=> validation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation
			? Location.Create(invocation.SyntaxTree, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(member.Name.SpanStart, invocation.Span.End))
			: validation.Syntax.GetLocation();
}
