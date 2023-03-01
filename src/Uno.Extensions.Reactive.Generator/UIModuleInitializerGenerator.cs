using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A generator that generates UI module initialization for the reactive framework.
/// </summary>
[Generator]
public sealed class UIModuleInitializerGenerator : IIncrementalGenerator, ICodeGenTool
{
	/// <inheritdoc />
	public string Version => "1";

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUGGING_GENERATOR
		var process = Process.GetCurrentProcess().ProcessName;
		if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
		{
			Debugger.Launch();
		}
#endif

		var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.Assembly.Name);
		var hasReactiveUIModuleInitializer = context.CompilationProvider.Select((compilation, _) => compilation.GetTypeByMetadataName("Uno.Extensions.Reactive.UI.ModuleInitializer") is not null);
		context.RegisterSourceOutput(assemblyNameProvider.Combine(hasReactiveUIModuleInitializer), (context, source) =>
		{
			if (source.Right)
			{
				var assembly = source.Left;
				context.AddSource(PathHelper.SanitizeFileName($"{assembly}.ReactiveUIModuleInitializer.g.cs"), $@"{this.GetFileHeader(4)}

namespace {assembly}
{{
	/// <summary>
	/// Initialize provider of dispatcher.
	/// </summary>
	/// <remarks>This class ensures that dispatcher has been initialized even if the Reactive.UI package has not been loaded yet.</remarks>
	[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
	{this.GetCodeGenAttribute()}
	internal static class __ReactiveUIModuleInitializer
	{{
		/// <summary>
		/// Register the <seealso cref=""DispatcherQueueProvider""/> as provider of <see cref=""IDispatcher""/> for the reactive platform.
		/// </summary>
		/// <remarks>This method is flagged with ModuleInitializer attribute and should not be used by application.</remarks>
		[global::System.Runtime.CompilerServices.ModuleInitializer]
		public static void Initialize()
		{{
			global::Uno.Extensions.Reactive.UI.ModuleInitializer.Initialize();
		}}
	}}
}}
".Align(0));
			}
		});
	}
}
