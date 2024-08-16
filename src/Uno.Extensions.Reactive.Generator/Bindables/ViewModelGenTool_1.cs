using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Extensions.Generators;
using Uno.Extensions.Reactive.Config;
using Uno.RoslynHelpers;
using static Microsoft.CodeAnalysis.Accessibility;

namespace Uno.Extensions.Reactive.Generator;

internal class ViewModelGenTool_1 : ICodeGenTool
{
	/// <inheritdoc />
	public string Version => "1";

	private readonly BindableGenerationContext _ctx;
	private readonly ViewModelGenerator_1 _bindables;
	private readonly BindableViewModelMappingGenerator _viewModelsMapping;
	private readonly IAssemblySymbol _assembly;

	public ViewModelGenTool_1(BindableGenerationContext ctx)
	{
		_ctx = ctx;
		_bindables = new ViewModelGenerator_1(ctx);
		_viewModelsMapping = new BindableViewModelMappingGenerator(ctx);
		_assembly = ctx.Context.Compilation.Assembly;
	}

	private bool IsSupported(INamedTypeSymbol? type)
		=> type is not null
			&& (_ctx.IsGenerationEnabled(type)
				?? type.Name.EndsWith("ViewModel", StringComparison.Ordinal) && type.IsPartial());

	public IEnumerable<(string fileName, string code)> Generate()
	{
		var viewModels = from module in _assembly.Modules
			from type in module.GetNamespaceTypes()
			where IsSupported(type)
			select type;

		foreach (var vm in viewModels)
		{
			yield return (vm.ToString(), Generate(vm));
		}

		foreach (var (type, code) in _bindables.Generate())
		{
			yield return (type.ToString(), code: code);
		}

		yield return _viewModelsMapping.Generate();
	}

	private static string GetViewModelName(INamedTypeSymbol type)
		=> $"Bindable{type.Name}";

	private string Generate(INamedTypeSymbol model)
	{
		var vmName = GetViewModelName(model);
		var hasBaseType = IsSupported(model.BaseType);
		var baseType = hasBaseType
			? $"{model.BaseType}.{GetViewModelName(model.BaseType!)}"
			: $"{NS.Bindings}.BindableViewModelBase";

		List<IInputInfo> inputs;
		IEnumerable<string> inputsErrors;
		if (hasBaseType || model.IsAbstract)
		{
			inputs = new List<IInputInfo>(0);
			inputsErrors = Enumerable.Empty<string>();
		}
		else
		{
			inputs = GetInputs(model).ToList();
			inputsErrors = inputs
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
		}

		var mappedMembers = GetMembers(model).ToList();
		var mappedMembersConflictingWithInputs = mappedMembers
			.Where(member => inputs.Any(input => input.Property?.Name.Equals(member.Name, StringComparison.Ordinal) ?? false))
			.ToList();
		var mappedMembersErrors = mappedMembersConflictingWithInputs
			.Select(member => $"// {member.Name} is not mapped from the Model as it would conflict with another member.")
			.ToList();
		mappedMembers = mappedMembers.Except(mappedMembersConflictingWithInputs).ToList();

		var bindableVmCode = $@"
			{this.GetCodeGenAttribute()}
			public partial class {vmName} : {baseType} 
			{{
				{inputsErrors.Align(4)}
				{inputs.Select(input => input.GetBackingField()).Align(4)}
				{mappedMembers.Select(member => member.GetBackingField()).Align(4)}

				{model
					.Constructors
					.Where(ctor => !ctor.IsCloneCtor(model)
						// we do not support inheritance of ctor, inheritance always goes through the same BindableVM(vm) ctor.
						&& ctor.DeclaredAccessibility is not Protected and not ProtectedAndFriend and not ProtectedAndInternal)
					.Where(_ctx.IsGenerationNotDisable)
					.Select(ctor =>
					{
						if (hasBaseType)
						{
							var parameters = ctor
								.Parameters
								.Select(p => new ParameterInput(p))
								.ToList();

							return $@"
								{GetCtorAccessibility(ctor)} {vmName}({parameters.Select(p => p.GetCtorParameter().code).JoinBy(", ")})
									: base(new {model}({parameters.Select(p => p.GetVMCtorParameter()).JoinBy(", ")}))
								{{
									var {N.Ctor.Model} = {N.Model};
									var {N.Ctor.Ctx} = {NS.Core}.SourceContext.GetOrCreate({N.Ctor.Model});

									{mappedMembers.Select(member => member.GetInitialization()).Align(9)}
								}}";
						}
						else
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
								{GetCtorAccessibility(ctor)} {vmName}({bindableVmParameters})
								{{
									{inputs.Select(input => input.GetCtorInit(parameters.Contains(input))).Align(9)}

									var {N.Ctor.Model} = new {model}({vmParameters});
									var {N.Ctor.Ctx} = {NS.Core}.SourceContext.GetOrCreate({N.Ctor.Model});
									{NS.Core}.SourceContext.Set(this, {N.Ctor.Ctx});
									base.RegisterDisposable({N.Ctor.Model});

									{N.Model} = {N.Ctor.Model};
									{inputs.Select(input => input.GetPropertyInit()).Align(9)}
									{mappedMembers.Select(member => member.GetInitialization()).Align(9)}
								}}";
						}
					})
					.Align(4)}

				protected {vmName}({model} {N.Ctor.Model}){(hasBaseType ? $" : base({N.Ctor.Model})" : "")}
				{{
					var {N.Ctor.Ctx} = {NS.Core}.SourceContext.GetOrCreate({N.Ctor.Model});
					{(hasBaseType ? "" : $"{NS.Core}.SourceContext.Set(this, {N.Ctor.Ctx});")}

					{mappedMembers.Select(member => member.GetInitialization()).Align(5)}
				}}

				public {(hasBaseType ? $"new {model} {N.Model} => ({model}) base.{N.Model};" : $"{model} {N.Model} {{ get; }}")}

				{inputs.Select(input => input.Property?.ToString()).Align(4)}

				{mappedMembersErrors.Align(4)}
				{mappedMembers.Select(member => member.GetDeclaration()).Align(4)}
			}}";

		var fileCode = this.AsPartialOf(
			model,
			attributes: null,
			bases: $"global::System.IAsyncDisposable, {NS.Core}.ISourceContextAware",
			code: $@"
				{bindableVmCode.Align(4)}

				/// <inheritdoc />
				{this.GetCodeGenAttribute()}
				public global::System.Threading.Tasks.ValueTask DisposeAsync()
					=> {NS.Core}.SourceContext.Find(this)?.DisposeAsync() ?? default;
			");

		// If type is at least internally accessible, add it to a mapping from the VM type to it's bindable counterpart to ease usage in navigation engine.
		// (Private types are almost only a test case which is not supported by nav anyway)
		if (model.DeclaredAccessibility is not Accessibility.Private
			&& model.GetContainingTypes().All(type => type.DeclaredAccessibility is not Accessibility.Private))
		{
			_viewModelsMapping.Register(model, $"{model.ContainingNamespace}.{model.GetContainingTypes().Select(type => type.Name + '.').JoinBy("")}{model.Name}.{vmName}");
		}

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
					yield return new BindableListFromListFeedField(field, valueType);
					break;

				case IFieldSymbol field when _ctx.IsFeedOfList(field.Type, out var collectionType, out var valueType):
					yield return new BindableListFromFeedOfListField(field, collectionType, valueType);
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
					yield return new BindableListFromListFeedProperty(property, valueType);
					break;

				case IPropertySymbol property when _ctx.IsFeedOfList(property.Type, out var collectionType, out var valueType):
					yield return new BindableListFromFeedOfListProperty(property, collectionType, valueType);
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

				case IMethodSymbol method when CommandFromMethod.TryCreate(type, method, _ctx, out var commandGenerator):
					yield return commandGenerator;
					break;

				case IMethodSymbol { MethodKind: MethodKind.Ordinary, IsImplicitlyDeclared: false } method:
					yield return new MappedMethod(method);
					break;
			}
		}
	}
}
