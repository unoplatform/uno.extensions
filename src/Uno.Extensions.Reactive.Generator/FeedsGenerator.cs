using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Uno.Extensions.Reactive.Generator.Utils;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A generator that generates bindable VM for the feed framework
/// </summary>
[Generator]
public partial class FeedsGenerator : ISourceGenerator
{
	/// <inheritdoc />
	public void Initialize(GeneratorInitializationContext context) { }

	/// <inheritdoc />
	public void Execute(GeneratorExecutionContext context)
	{
#if DEBUGGING_GENERATOR || true
		var process = Process.GetCurrentProcess().ProcessName;
		if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
		{
			Debugger.Launch();
		}
#endif

		if (BindableGenerationContext.TryGet(context, out var error) is {} bindableContext)
		{
			foreach (var generated in new BindableViewModelGenerator(bindableContext).Generate(context.Compilation.Assembly))
			{
				context.AddSource(PathHelper.SanitizeFileName(generated.fileName), generated.code);
			}
		}
		else
		{
			//context.GetLogger().Error(error);
			throw new InvalidOperationException(error);
		}
	}
}

//[DiagnosticAnalyzer("cs")]
//public partial class Feed2001Analyzer : DiagnosticAnalyzer
//{
//	/// <inheritdoc />
//	public override void Initialize(AnalysisContext context)
//	{
//		context.EnableConcurrentExecution();
//		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
//		//context.RegisterSymbolAction(ctx => ctx.ReportDiagnostic(Rules.FEED2001.Descriptor).Symbol.ReportDiagnostic(), SymbolKind.Method);
//	}

//	/// <inheritdoc />
//	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rules.FEED2001.Descriptor);
//}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Rules
{
	// references:
	//	Categories: https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/categories

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
		private const string message = "The property '{0}' resolved on the class '{1}' cannot be used as command parameter for '{2}' as it's not a Feed";

		public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
			nameof(FEED2002),
			"Invalid property type",
			message,
			Category.Usage,
			DiagnosticSeverity.Error,
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

internal static class Category
{
	// cf. https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/categories

	/// <summary>
	/// Usage rules support proper usage of .NET.
	/// </summary>
	public const string Usage = "Usage";
}

//public partial class Feed001Fixer : CodeFixProvider
//{
//	/// <inheritdoc />
//	public override Task RegisterCodeFixesAsync(CodeFixContext context)
//	{
//		//context.RegisterCodeFix(CodeAction.Create()"bla", ct => ),  );
//	}

//	/// <inheritdoc />
//	public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("FEED0001");
//}
