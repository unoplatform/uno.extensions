using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Navigation.Generators;

internal class ForceBindingsUpdateGenTool_1 : ICodeGenTool
{
	/// <inheritdoc />
	public string Version => "1";

	private readonly ForceBindingsUpdateGenerationContext _ctx;
	private readonly IAssemblySymbol _assembly;

	public ForceBindingsUpdateGenTool_1(ForceBindingsUpdateGenerationContext ctx)
	{
		_ctx = ctx;
		_assembly = ctx.Context.Compilation.Assembly;
	}

	private bool IsSupported(INamedTypeSymbol? type)
		=> type is not null
			&& (_ctx.IsGenerationEnabled(type)
				?? (
						type.Is(_ctx.Page) &&
						type.IsPartial()
					))
		&& _ctx.ContainsXBind(type);

	public IEnumerable<(string fileName, string code)> Generate()
	{
		var pages = from module in _assembly.Modules
					from type in module.GetNamespaceTypes()
					where
						type is not null &&
						IsSupported(type)
					select type;

		foreach (var page in pages)
		{
			yield return (page.ToDisplayString(), Generate(page));
		}
	}

	private string Generate(INamedTypeSymbol model)
	{
		var className = model.Name;

		var updateInterface = _ctx.ForceBindingsUpdateInterface.ToDisplayString();
		var fileCode = this.AsPartialOf(
			model,
			attributes: default,
			bases: updateInterface,
			code: $@"
				ValueTask IForceBindingsUpdate.ForceBindingsUpdateAsync()
				{{	
					this.Bindings.Update();
					return ValueTask.CompletedTask;
				}}
			");

		return fileCode.Align(0);
	}


}
