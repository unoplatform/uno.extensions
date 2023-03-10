using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Uno.Extensions.Generators.PropertySelector;

namespace Uno.Extensions.Generators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class PropertySelectorAnalyzer : DiagnosticAnalyzer
{
	private sealed class Visitor : CSharpSyntaxVisitor
	{
		private readonly OperationAnalysisContext _context;
		private readonly SimpleLambdaExpressionSyntax _selectorSyntax;
		private bool _isFirstIdentifier;

		public Visitor(OperationAnalysisContext context, SimpleLambdaExpressionSyntax selectorSyntax)
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
				if (_selectorSyntax.Parameter.Identifier.ValueText != node.Identifier.ValueText)
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

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		Rules.PS0001.Descriptor,
		Rules.PS0002.Descriptor,
		Rules.PS0003.Descriptor,
		Rules.PS0004.Descriptor,
		Rules.PS0005.Descriptor,
		Rules.PS0006.Descriptor,
		Rules.PS0102.Descriptor);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.RegisterCompilationStartAction(context =>
		{
			var propertySelectorSymbol = context.Compilation.GetTypeByMetadataName("Uno.Extensions.Edition.PropertySelector`2");
			var callerFilePathSymbol = context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerFilePathAttribute");
			var callerlineNumberSymbol = context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CallerLineNumberAttribute");
			if (propertySelectorSymbol is not null && callerFilePathSymbol is not null && callerlineNumberSymbol is not null)
			{
				context.RegisterOperationAction(context => AnalyzerInvocation(context, propertySelectorSymbol, callerFilePathSymbol, callerlineNumberSymbol), OperationKind.Invocation);
			}
		});
	}

	private static void AnalyzerInvocation(OperationAnalysisContext context, INamedTypeSymbol propertySelectorSymbol, INamedTypeSymbol callerFilePathSymbol, INamedTypeSymbol callerLineNumberSymbol)
	{
		var operation = (IInvocationOperation)context.Operation;
		var method = operation.TargetMethod;
		if (!IsCandidate(method, propertySelectorSymbol, callerFilePathSymbol, callerLineNumberSymbol, out var selectorParameter, out var callerFileParameter, out var callerLineParameter))
		{
			return;
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

		if (selectorArg.Expression is SimpleLambdaExpressionSyntax simpleLambdaExpression)
		{
			var path = PropertySelectorPathResolver.Resolve(simpleLambdaExpression);
			var count = path.Parts.Count;
			var type = entityType;

			if (entityTypeRecord.NullableAnnotation != NullableAnnotation.NotAnnotated &&
				!entityTypeRecord.Constructors.Any(ctor => ctor.IsAccessible() && !ctor.IsCloneCtor(entityTypeRecord) && ctor.Parameters.All(HasDefault)))
			{
				context.ReportDiagnostic(Rules.PS0006.GetDiagnostic(path.FullPath, path.Parts[0].Name, path.Parts[0].Node, type));
			}

			if (simpleLambdaExpression.ExpressionBody is not null)
			{
				new Visitor(context, simpleLambdaExpression).Visit(simpleLambdaExpression.ExpressionBody);
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
		selectorParameter = method.Parameters.FirstOrDefault(p => p.Type.OriginalDefinition.Equals(propertySelectorSymbol, SymbolEqualityComparer.Default));
		if (selectorParameter is null)
		{
			callerFileParameter = null;
			callerLineParameter = null;
			return false;
		}

		callerFileParameter = method.Parameters.FirstOrDefault(p => p.FindAttribute(callerFilePathSymbol) is not null);
		callerLineParameter = method.Parameters.FirstOrDefault(p => p.FindAttribute(callerLineNumberSymbol) is not null);
		return callerFileParameter is not null && callerLineParameter is not null;
	}
}
