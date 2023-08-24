using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

[Generator]
internal sealed class HotReloadModuleInitializerGenerator : IIncrementalGenerator, ICodeGenTool
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
		var hasFeedConfiguration = context.CompilationProvider.Select((compilation, _) => compilation.GetTypeByMetadataName($"{NS.Config}.FeedConfiguration") is not null);

		context.RegisterSourceOutput(
			assemblyNameProvider.Combine(hasFeedConfiguration),
			(ctx, source) =>
			{
				if (source.Right)
				{
					var assembly = source.Left;
					ctx.AddSource(
						PathHelper.SanitizeFileName($"{assembly}.ReactiveHotReloadModuleInitializer.g.cs"),
						GetSource(assembly));
				}
			});
	}

	private string GetSource(string assembly)
		=> $@"{this.GetFileHeader(4)}

namespace {assembly}
{{
	/// <summary>
	/// Initialize hot-reload support.
	/// </summary>
	/// <remarks>This class ensures that the hot-reload support is enabled only when needed (i.e. !IS_HOT_RELOAD_DISABLED && (DEBUG || IS_HOT_RELOAD_ENABLED).</remarks>
	[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
	{this.GetCodeGenAttribute()}
	internal static class __ReactiveHotReloadModuleInitializer
	{{
		/// <summary>
		/// Configures hot-reload for MVUX.
		/// </summary>
		/// <remarks>This method is flagged with ModuleInitializer attribute and should not be used by application.</remarks>
		[global::System.Runtime.CompilerServices.ModuleInitializer]
		public static void Initialize()
		{{
#if !IS_HOT_RELOAD_DISABLED && (DEBUG || IS_HOT_RELOAD_ENABLED)
			var isEnabled = true;
#else
			var isEnabled = false;
#endif
			global::{NS.Config}.ModuleConfiguration.ConfigureHotReload(""{assembly}"", isEnabled);
		}}
	}}
}}
".Align(0);
}
