using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Uno.Extensions.Generators.PropertySelector;

namespace Uno.Extensions.Generators.Analyzers;

/// <summary>
/// Analyzer for common mistakes when using PropertySelectorGenerator.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PropertySelectorAnalyzer : DiagnosticAnalyzer
{
	private sealed class Visitor : CSharpSyntaxVisitor
	{
		private readonly OperationAnalysisContext _context;
		private readonly ParenthesizedLambdaExpressionSyntax _selectorSyntax;
		private bool _isFirstIdentifier = true;

		public Visitor(OperationAnalysisContext context, ParenthesizedLambdaExpressionSyntax selectorSyntax)
		{
			_context = context;
			_selectorSyntax = selectorSyntax;
		}

		public override void DefaultVisit(SyntaxNode node)
		{
			// We get to DefaultVisit when we are visiting a node we haven't overridden its visit method.
			// We shouldn't call base.VisitXXX(..) in any override, otherwise, DefaultVisit will be called.
			_context.ReportDiagnostic(Rules.PS0001.GetDiagnostic(_selectorSyntax, node));
		}

		public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
		{
			Visit(node.Expression);
			Visit(node.WhenNotNull);
		}

		public override void VisitIdentifierName(IdentifierNameSyntax node)
		{
			if (_isFirstIdentifier)
			{
				if (_selectorSyntax.ParameterList.Parameters[0].Identifier.ValueText != node.Identifier.ValueText)
				{
					_context.ReportDiagnostic(Rules.PS0002.GetDiagnostic(_selectorSyntax, node));
				}
				_isFirstIdentifier = false;
			}
		}

		public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
		{
			Visit(node.Expression);
		}

		public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
		{
		}

		public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
		{
			Visit(node.Operand);
			if (!node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
			{
				_context.ReportDiagnostic(Rules.PS0001.GetDiagnostic(_selectorSyntax, node));
			}
		}
	}

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		Rules.PS0001.Descriptor,
		Rules.PS0002.Descriptor,
		Rules.PS0003.Descriptor,
		Rules.PS0004.Descriptor,
		Rules.PS0005.Descriptor,
		Rules.PS0006.Descriptor,
		Rules.PS0007.Descriptor,
		Rules.PS0102.Descriptor);

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.RegisterCompilationStartAction(context =>
		{
			var propertySelectorSymbol = context.Compilation.GetTypeByMetadataName("Uno.Extensions.Edition.PropertySelector`2");
			var propertySelectorAttributeType = context.Compilation.GetTypeByMetadataName("Uno.Extensions.Edition.PropertySelectorAttribute");
			var callerFilePathSymbol = context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerFilePathAttribute");
			var callerlineNumberSymbol = context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerLineNumberAttribute");
			if (propertySelectorSymbol is not null && callerFilePathSymbol is not null && callerlineNumberSymbol is not null)
			{
				context.RegisterOperationAction(context => AnalyzeInvocation(context, propertySelectorSymbol, callerFilePathSymbol, callerlineNumberSymbol, propertySelectorAttributeType), OperationKind.Invocation);
			}
		});
	}

	private static void AnalyzeInvocation(
		OperationAnalysisContext context, INamedTypeSymbol propertySelectorSymbol, INamedTypeSymbol callerFilePathSymbol, INamedTypeSymbol callerLineNumberSymbol, INamedTypeSymbol? propertySelectorAttributeType)
	{
		var operation = (IInvocationOperation)context.Operation;
		var method = operation.TargetMethod;
		if (!IsCandidate(method, propertySelectorSymbol, callerFilePathSymbol, callerLineNumberSymbol, out var selectorParameter, out var callerFileParameter, out var callerLineParameter))
		{
			return;
		}

		var argument = operation.Arguments.FirstOrDefault(arg => arg.Parameter?.Ordinal == selectorParameter.Ordinal);
		if (argument?.Value is IDelegateCreationOperation delegateCreationOperation &&
			delegateCreationOperation.Target is IAnonymousFunctionOperation anonymousFunctionOperation &&
			anonymousFunctionOperation.Symbol is { } symbol)
		{
			if (!symbol.GetAttributes().Any(a => a.AttributeClass?.Equals(propertySelectorAttributeType, SymbolEqualityComparer.Default) == true))
			{
				context.ReportDiagnostic(Rules.PS0007.GetDiagnostic(argument.Syntax));
				return;
			}
		}

		var callerFileArgument = operation.Arguments.FirstOrDefault(arg => arg.Parameter?.Ordinal == callerFileParameter.Ordinal);
		var callerLineArgument = operation.Arguments.FirstOrDefault(arg => arg.Parameter?.Ordinal == callerLineParameter.Ordinal);
		ReportDiagnosticForCallerAttribute(context, callerFileArgument, method, Rules.PS0102.GetFileArgDiagnostic);
		ReportDiagnosticForCallerAttribute(context, callerLineArgument, method, Rules.PS0102.GetLineArgDiagnostic);

		var selectorParameterType = (INamedTypeSymbol)selectorParameter.Type;
		var entityType = selectorParameterType.TypeArguments[0];
		var propertyType = selectorParameterType.TypeArguments[1];
		if (entityType is null or { Kind: SymbolKind.ErrorType }
			|| propertyType is null or { Kind: SymbolKind.ErrorType })
		{
			return;
		}

		var selectorArg = (ArgumentSyntax)operation.Arguments.First(arg => arg.Parameter?.Ordinal == selectorParameter.Ordinal).Syntax;
		if (entityType is not INamedTypeSymbol { IsRecord: true } entityTypeRecord)
		{
			context.ReportDiagnostic(Rules.PS0004.GetDiagnostic(selectorArg, entityType));
			return;
		}

		if (selectorArg.Expression is ParenthesizedLambdaExpressionSyntax lambdaExpression &&
			lambdaExpression.ParameterList.Parameters.Count == 1)
		{
			var path = PropertySelectorPathResolver.Resolve(lambdaExpression);
			var count = path.Parts.Count;
			var type = entityType;

			if (count > 0 &&
				entityTypeRecord.NullableAnnotation != NullableAnnotation.NotAnnotated &&
				!entityTypeRecord.Constructors.Any(ctor => ctor.IsAccessible() && !ctor.IsCloneCtor(entityTypeRecord) && ctor.Parameters.All(HasDefault)))
			{
				context.ReportDiagnostic(Rules.PS0006.GetDiagnostic(path.FullPath, path.Parts[0].Name, path.Parts[0].Node, type));
			}

			if (lambdaExpression.ExpressionBody is not null)
			{
				new Visitor(context, lambdaExpression).Visit(lambdaExpression.ExpressionBody);
			}
			

			for (var i = 1; i <= count; i++)
			{
				var part = path.Parts[i - 1];

				if (i < count) // 'current_x' is a parameter of the delegate for the leaf element.
				{
					type = (type as INamedTypeSymbol)?.FindProperty(part.Name)?.Type;
					if (type is null)
					{
						break;
					}

					if (type.NullableAnnotation == NullableAnnotation.NotAnnotated)
					{
						continue;
					}

					if (type is not INamedTypeSymbol { IsRecord: true } record)
					{
						context.ReportDiagnostic(Rules.PS0005.GetDiagnostic(path.FullPath, part.Name, part.Node, type));
					}
					else if (!record.Constructors.Any(ctor => ctor.IsAccessible() && !ctor.IsCloneCtor(record) && ctor.Parameters.All(HasDefault)))
					{
						context.ReportDiagnostic(Rules.PS0006.GetDiagnostic(path.FullPath, part.Name, part.Node, type));
					}
				}
			}
		}
		else
		{
			context.ReportDiagnostic(Rules.PS0003.GetDiagnostic(selectorArg));
		}

		bool HasDefault(IParameterSymbol parameter)
		{
			return parameter.HasExplicitDefaultValue || parameter.Type.NullableAnnotation == NullableAnnotation.Annotated || parameter.Type.IsNullable();
		}
	}

	private static void ReportDiagnosticForCallerAttribute(OperationAnalysisContext context, IArgumentOperation? argument, IMethodSymbol method, Func<IMethodSymbol, IParameterSymbol?, SyntaxNode, Diagnostic> diagnosticGetter)
	{
		if (argument?.Syntax is ArgumentSyntax argumentSyntax && argumentSyntax.Expression is not LiteralExpressionSyntax)
		{
			context.ReportDiagnostic(diagnosticGetter(method, argument.Parameter, argument.Syntax));
		}
	}

	private static bool IsCandidate(
		IMethodSymbol method,
		INamedTypeSymbol propertySelectorSymbol,
		INamedTypeSymbol callerFilePathSymbol,
		INamedTypeSymbol callerLineNumberSymbol,
		[NotNullWhen(true)] out IParameterSymbol? selectorParameter,
		[NotNullWhen(true)] out IParameterSymbol? callerFileParameter,
		[NotNullWhen(true)] out IParameterSymbol? callerLineParameter)
	{
		return PropertySelectorCandidate.IsCandidate(
			method,
			isPropertySelectorParameter: p => p.Type.OriginalDefinition.Equals(propertySelectorSymbol, SymbolEqualityComparer.Default),
			isCallerFilePathParameter: p => p.FindAttribute(callerFilePathSymbol) is not null,
			isCallerLineNumberParameter: p => p.FindAttribute(callerLineNumberSymbol) is not null,
			out selectorParameter,
			out callerFileParameter,
			out callerLineParameter);
	}
}
