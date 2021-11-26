using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal class BindableViewModelGenerator
{
	private readonly BindableGenerationContext _ctx;
	private readonly BindableGenerator _bindables;

	public BindableViewModelGenerator(BindableGenerationContext ctx)
	{
		_ctx = ctx;
		_bindables = new BindableGenerator(ctx);
	}

	private bool IsSupported(INamedTypeSymbol type)
		=> _ctx.IsGenerationEnabled(type)
			?? type
				.Constructors
				.SelectMany(ctor => ctor.Parameters)
				.Any(parameter => _ctx.IsInputOrCommand(parameter.Type));

	public IEnumerable<(INamedTypeSymbol type, string code)> Generate(IAssemblySymbol assembly)
	{
		var viewModels = from module in assembly.Modules
			from type in module.GetNamespaceTypes()
			where IsSupported(type)
			select type;

		foreach (var vm in viewModels)
		{
			yield return (vm, Generate(vm));
		}

		foreach (var bindable in _bindables.Generate())
		{
			yield return bindable;
		}
	}

	private string Generate(INamedTypeSymbol vm)
	{
		var mappedMembers = GetMembers(vm).ToList();
		var inputs = GetInputs(vm).ToList();
		var inputsErrors  = inputs
			.GroupBy(input => input.Parameter.Name)
			.Where(group => group.Distinct().Count() > 1)
			.Select(conflictingInput => 
			{
				var conflictingDeclarations = conflictingInput
					.Distinct()
					.Select(input => $"'{input.Parameter.Type}' [{input.Parameter.GetDeclaringLocationsDisplayString()}]")
					.JoinBy(", ");

				return $"#error The input named '{conflictingInput.Key}' does not have the same type in all declared constructors (found types {conflictingDeclarations}).";
			});
		inputs = inputs.Distinct().ToList();

		var code = $@"#nullable enable
#pragma warning disable

using System;
using System.Linq;
using System.Threading.Tasks;

namespace {vm.ContainingNamespace};

partial class {vm.Name} : global::System.IAsyncDisposable
{{
	public class Bindable{vm.Name} : {NS.Reactive}.BindableViewModelBase
	{{
		{inputsErrors.Align(2)}
		{inputs.Select(input => input.GetBackingField()).Align(2)}

		{vm
			.Constructors
			.Where(_ctx.IsGenerationNotDisable)
			.Select(ctor =>
			{
				var parameters = ctor
					.Parameters
					.Select(parameter => inputs.First(input => input.Parameter.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)))
					.ToArray();

				var bindableVmParameters = parameters
					.Select(param => param.GetCtorParameter())
					.Where(param => param.code is not null)
					.OrderBy(param => param.isOptional ? 1 : 0)
					.Select(param => param.code)
					.JoinBy(", ");
				var vmParameters = parameters
					.Select(param => param.GetVMCtorParameter())
					.JoinBy(", ");

				return $@"
					{GetCtorAccessibility(ctor)} Bindable{vm.Name}({bindableVmParameters})
					{{
						{inputs.Select(input => input.GetCtorInit(parameters.Contains(input))).Align(6)}

						var {N.Ctor.Model} = new {vm}({vmParameters});
						var {N.Ctor.Ctx} = {NS.Reactive}.SourceContext.GetOrCreate({N.Ctor.Model});
						{NS.Reactive}.SourceContext.Set(this, {N.Ctor.Ctx});
						base.RegisterDisposable({N.Ctor.Model});

						{N.Model} = {N.Ctor.Model};
						{inputs.Select(input => input.GetPropertyInit()).Align(6)}
						{mappedMembers.Select(member => member.GetInitialization()).Align(6)}
					}}";
			})
			.Align(2)}

		public {vm} {N.Model} {{ get; }}

		{inputs.Select(input => input.GetProperty()).Align(2)}

		{mappedMembers.Select(member => member.GetDeclaration()).Align(2)}
	}}

	/// <inheritdoc />
	public global::System.Threading.Tasks.ValueTask DisposeAsync()
		=> {NS.Reactive}.SourceContext.Find(this)?.DisposeAsync() ?? default;
}}
";

		return code;
	}

	private string GetCtorAccessibility(IMethodSymbol ctor)
		=> ctor.DeclaredAccessibility == Accessibility.Private
			? "public"
			: ctor.GetAccessibilityAsCSharpCodeString();

	private IEnumerable<IInputInfo> GetInputs(INamedTypeSymbol type)
		=> type.Constructors.SelectMany(GetInputs).ToList();

	private IEnumerable<IInputInfo> GetInputs(IMethodSymbol vmCtor)
	{
		var parameters = vmCtor.Parameters;

		foreach (var parameter in parameters)
		{
			if (_ctx.IsFeed(parameter, out var valueType, out var kind))
			{
				switch (kind)
				{
					case InputKind.External:
						yield return new ParameterInput(parameter);
						break;

					case InputKind.Edit when _bindables.GetBindableType(valueType) is { } bindableType:
						yield return new BindableInput(parameter, valueType, bindableType);
						break;

					case InputKind.Value:
					default:
						yield return new FeedInput(parameter, valueType);
						break;
				}
			}
			else if (_ctx.IsCommand(parameter.Type, out var commandParameterType))
			{
				yield return new CommandInput(parameter, commandParameterType);
			}
			else
			{
				yield return new ParameterInput(parameter);
			}
		}
	}

	private IEnumerable<IMappedMember> GetMembers(INamedTypeSymbol type)
	{
		foreach (var member in type.GetMembers().Where(member => member.IsAccessible() && !member.IsStatic))
		{
			switch (member)
			{
				case IFieldSymbol field when _ctx.IsFeed(field.Type, out var valueType):
					yield return new MappedFeedField(field, valueType);
					break;

				case IFieldSymbol field:
					yield return new MappedField(field);
					break;

				case IPropertySymbol property when _ctx.IsFeed(property.Type, out var valueType):
					yield return new MappedFeedProperty(property, valueType);
					break;

				case IPropertySymbol property:
					yield return new MappedProperty(property);
					break;

				case IMethodSymbol { MethodKind: MethodKind.Ordinary, IsImplicitlyDeclared: false } method:
					yield return new MappedMethod(method);
					break;
			}
		}
	}
}
