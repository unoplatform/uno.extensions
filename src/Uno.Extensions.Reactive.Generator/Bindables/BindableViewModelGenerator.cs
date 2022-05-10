using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.RoslynHelpers;

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
			?? type.Name.EndsWith("ViewModel", StringComparison.Ordinal) && type.IsPartial();

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
		var inputs = GetInputs(vm).ToList();
		var inputsErrors = inputs
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

		var mappedMembers = GetMembers(vm).ToList();
		var mappedMembersConflictingWithInputs = mappedMembers
			.Where(member => inputs.Any(input => input.Property?.Name.Equals(member.Name, StringComparison.Ordinal) ?? false))
			.ToList();
		var mappedMembersErrors = mappedMembersConflictingWithInputs
			.Select(member => $"// {member.Name} is not mapped from the Model as it would conflict with another member.")
			.ToList();
		mappedMembers = mappedMembers.Except(mappedMembersConflictingWithInputs).ToList();


		var bindableVmCode = $@"
			public partial class Bindable{vm.Name} : {NS.Bindings}.BindableViewModelBase
			{{
				{inputsErrors.Align(4)}
				{inputs.Select(input => input.GetBackingField()).Align(4)}
				{mappedMembers.Select(member => member.GetBackingField()).Align(4)}

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
								{inputs.Select(input => input.GetCtorInit(parameters.Contains(input))).Align(8)}

								var {N.Ctor.Model} = new {vm}({vmParameters});
								var {N.Ctor.Ctx} = {NS.Core}.SourceContext.GetOrCreate({N.Ctor.Model});
								{NS.Core}.SourceContext.Set(this, {N.Ctor.Ctx});
								base.RegisterDisposable({N.Ctor.Model});

								{N.Model} = {N.Ctor.Model};
								{inputs.Select(input => input.GetPropertyInit()).Align(8)}
								{mappedMembers.Select(member => member.GetInitialization()).Align(8)}
							}}";
					})
					.Align(4)}

				public {vm} {N.Model} {{ get; }}

				{inputs.Select(input => input.Property?.ToString()).Align(4)}

				{mappedMembersErrors.Align(4)}
				{mappedMembers.Select(member => member.GetDeclaration()).Align(4)}
			}}";

		// We make the bindbale VM a nested class of the VM itself
		var vmCode = $@"partial {(vm.IsRecord ? "record" : "class")} {vm.Name} : global::System.IAsyncDisposable, {NS.Core}.ISourceContextAware
			{{
				{bindableVmCode.Align(4)}

				/// <inheritdoc />
				public global::System.Threading.Tasks.ValueTask DisposeAsync()
					=> {NS.Core}.SourceContext.Find(this)?.DisposeAsync() ?? default;
			}}";

		// Inject usings and declare the full namespace, including class nesting
		var fileCode = $@"#nullable enable
			#pragma warning disable

			using System;
			using System.Linq;
			using System.Threading.Tasks;

			namespace {vm.ContainingNamespace}
			{{
				{vm.GetContainingTypes().Select(type => $"partial {(type.IsRecord ? "record" : "class")} {type.Name}\r\n{{").Align(4)}
				{vmCode.Align(4).Indent(vm.GetContainingTypes().Count())}
				{vm.GetContainingTypes().Select(_ => "}").Align(4)}
			}}
			";

		return fileCode.Align(0);
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
				case IFieldSymbol field when _ctx.IsListFeed(field.Type, out var valueType):
					yield return new BindableListFeedField(field, valueType);
					break;

				case IFieldSymbol field when _ctx.IsFeed(field.Type, out var valueType):
				{
					yield return _bindables.GetBindableType(valueType) is { } bindableType && !field.HasAttributes(_ctx.ValueAttribute)
						? new BindableFromFeedField(field, valueType, bindableType)
						: new PropertyFromFeedField(field, valueType);
					break;
				}

				case IFieldSymbol field:
					yield return new MappedField(field);
					break;

				case IPropertySymbol property when _ctx.IsListFeed(property.Type, out var valueType):
					yield return new BindableListFeedProperty(property, valueType);
					break;

				case IPropertySymbol property when _ctx.IsFeed(property.Type, out var valueType):
				{
					yield return _bindables.GetBindableType(valueType) is { } bindableType && !property.HasAttributes(_ctx.ValueAttribute)
						? new BindableFromFeedProperty(property, valueType, bindableType)
						: new PropertyFromFeedProperty(property, valueType);
					break;
				}

				case IPropertySymbol property:
					yield return new MappedProperty(property);
					break;

				case IMethodSymbol method when CommandFromMethod.IsSupported(method, _ctx):
					yield return new CommandFromMethod(method, _ctx);
					break;

				case IMethodSymbol { MethodKind: MethodKind.Ordinary, IsImplicitlyDeclared: false } method:
					yield return new MappedMethod(method);
					break;
			}
		}
	}
}
