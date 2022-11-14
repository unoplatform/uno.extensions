using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator.Dispatching
{
	internal class DispatcherInitializerGenerationTool : ICodeGenTool
	{
		private readonly DispatcherInitializerGenerationContext _ctx;

		/// <inheritdoc />
		public string Version => "1";

		public DispatcherInitializerGenerationTool(DispatcherInitializerGenerationContext context)
		{
			_ctx = context;
		}


		public IEnumerable<GeneratedFile> Generate()
		{
			if (_ctx.DispatcherProvider is not null)
			{
				var assembly = _ctx.Context.Compilation.Assembly.Name;
				yield return new(
					$"{assembly}.DispatcherInitializer",
					@$"{this.GetFileHeader(5)}

						namespace {assembly}
						{{
							/// <summary>
							/// Initialize provider of dispatcher.
							/// </summary>
							/// <remarks>This class ensures that dispatcher has been initialized even if the Reactive.UI package has not been loaded yet.</remarks>
							[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
							{this.GetCodeGenAttribute()}
							internal static class __DispatcherInitializer
							{{
								/// <summary>
								/// Register the <seealso cref=""DispatcherQueueProvider""/> as provider of <see cref=""IDispatcher""/> for the reactive platform.
								/// </summary>
								/// <remarks>This method is flagged with ModuleInitializer attribute and should not be used by application.</remarks>
								[global::System.Runtime.CompilerServices.ModuleInitializer]
								public static void Initialize()
								{{
									global::Uno.Extensions.Reactive.Dispatching.DispatcherQueueProvider.Initialize();
								}}
							}}
						}}".Align(0));
			}
		}
	}
}
