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

internal class ViewModelGenTool_2 : ICodeGenTool
{
	/// <inheritdoc />
	public string Version => "2";

	private readonly BindableGenerationContext _ctx;
	private readonly BindableGenerator _bindables;
	private readonly BindableViewModelMappingGenerator _viewModelsMapping;
	private readonly IAssemblySymbol _assembly;

	public ViewModelGenTool_2(BindableGenerationContext ctx)
	{
		_ctx = ctx;
		_bindables = new BindableGenerator(ctx);
		_viewModelsMapping = new BindableViewModelMappingGenerator(ctx);
		_assembly = ctx.Context.Compilation.Assembly;
	}

	private bool IsSupported([NotNullWhen(true)] INamedTypeSymbol? type)
	{
		if (type is null)
		{
			return false;
		}

		if (_ctx.IsGenerationEnabled(type) is {} isEnabled)
		{
			// If the attribute is set, we don't check for the `partial`: the build as to fail if not
			return isEnabled;
		}

		if (type.IsPartial()
			&& (type.ContainingAssembly.FindAttribute<ImplicitBindablesAttribute>() ?? new ()) is { IsEnabled: true } @implicit // Note: the type might be from another assembly than current
			&& @implicit.Patterns.Any(pattern => Regex.IsMatch(type.ToString(), pattern)))
		{
			return true;
		}

		return false;
	}

	public IEnumerable<(string fileName, string code)> Generate()
	{
		var models = from module in _assembly.Modules
			from type in module.GetNamespaceTypes()
			where IsSupported(type)
			select type;

		foreach (var model in models)
		{
			yield return (model + ".Bindable", GenerateViewModel(model));
			yield return (model.ToString(), GeneratePartialModel(model));
		}

		foreach (var (type, code) in _bindables.Generate())
		{
			yield return (type.ToString(), code: code);
		}

		yield return _viewModelsMapping.Generate();
	}

	private static string GetViewModelName(INamedTypeSymbol model)
		=> $"Bindable{model.Name}";

	private static string GetViewModelFullName(INamedTypeSymbol model)
		=> $"{model.ToFullString().TrimEnd(model.Name, StringComparison.Ordinal)}Bindable{model.Name}";

	private string GenerateViewModel(INamedTypeSymbol model)
	{
		var vmName = GetViewModelName(model);
		var hasBaseType = IsSupported(model.BaseType);
		var baseType = hasBaseType
			? GetViewModelFullName(model.BaseType!)
			: $"{NS.Bindings}.BindableViewModelBase";

		var members = GetMembers(model).ToList();
		var vm = this.InSameNamespaceOf(
			model,
			$@"
				{this.GetCodeGenAttribute()}
				[{NS.Bindings}.Bindable(typeof({model.ToFullString()}))]
				{model.DeclaredAccessibility.ToCSharpCodeString()} partial class {vmName} : {baseType} 
				{{
					{members.Select(member => member.GetBackingField()).Align(5)}

					{model
						.Constructors
						.Where(ctor => !ctor.IsCloneCtor(model)
							// we do not support inheritance of ctor, inheritance always goes through the same BindableVM(vm) ctor.
							&& ctor.DeclaredAccessibility is not Protected and not ProtectedAndFriend and not ProtectedAndInternal and not Private)
						.Where(_ctx.IsGenerationNotDisable)
						.Select(ctor => $@"
							{GetCtorAccessibility(ctor)} {vmName}({ctor.Parameters.Select(p => p.ToFullString()).JoinBy(", ")})
								: this(new {model.ToFullString()}({ctor.Parameters.Select(p => p.Name).JoinBy(", ")}))
							{{
								if ({NS.Config}.FeedConfiguration.EffectiveHotReload.HasFlag({NS.Config}.HotReloadSupport.State))
								{{
									__reactiveModelArgs = new (Type, string, object?)[] {{ {ctor.Parameters.Select(p => $"(typeof({p.Type.ToFullString()}), \"{p.Name}\", {p.Name} as object)").JoinBy(", ")} }};
								}}
							}}")
						.Align(5)}

					protected {vmName}({model.ToFullString()} {N.Ctor.Model}){(hasBaseType ? $" : base({N.Ctor.Model})" : "")}
					{{
						var {N.Ctor.Ctx} = {NS.Core}.SourceContext.GetOrCreate({N.Ctor.Model});

						{(hasBaseType ? "" : @$"// Share the context between Model and ViewModel
							{NS.Core}.SourceContext.Set(this, {N.Ctor.Ctx});
							base.RegisterDisposable({N.Ctor.Model});
							{N.Model} = {N.Ctor.Model};").Align(6)}

						{N.Ctor.Model}.__reactiveBindableViewModel = this;

						{members.Select(member => member.GetInitialization()).Align(6)}

						{(hasBaseType ? "" : $@"
							if ({N.Ctor.Model} is global::System.ComponentModel.INotifyPropertyChanged npc)
							{{
								npc.PropertyChanged += __Reactive_OnModelPropertyChanged;
							}}").Align(6)}
					}}

					#region Hot-reload support
					private (Type type, string name, object? value)[]? __reactiveModelArgs;

					protected override (Type type, string name, object? value)[] __Reactive_GetModelArguments()
						=> __reactiveModelArgs ?? base.__Reactive_GetModelArguments();

					#if {!hasBaseType}
					protected override void __Reactive_UpdateModel(object updatedModel)
					{{
						if ({N.Model} is global::System.ComponentModel.INotifyPropertyChanged npc)
						{{
							npc.PropertyChanged -= __Reactive_OnModelPropertyChanged;
						}}

						var previousModel = (object){N.Model};

						__Reactive_BindableInitializeForUpdatedModel(updatedModel, {NS.Core}.SourceContext.GetOrCreate(updatedModel));
						__Reactive_TryPatchBindableProperties(previousModel, updatedModel);

						base.RaisePropertyChanged(""""); // 'Model' and any other mapped property.
					}}
					#endif

					protected {(hasBaseType ? "override" : "virtual")} void __Reactive_BindableInitializeForUpdatedModel(object updatedModel, {NS.Core}.SourceContext {N.Ctor.Ctx})
					{{
						#if {hasBaseType}
						base.__Reactive_BindableInitializeForUpdatedModel(updatedModel, {N.Ctor.Ctx});
						#else
						//{N.Model} = model;
						#endif

						dynamic {N.Ctor.Model} = updatedModel;

						{N.Ctor.Model}.__reactiveBindableViewModel = this;

						{members.Select(member => $@"try
							{{
								{member.GetInitialization()}
							}}
							catch (Exception)
							{{
								if (__Reactive_Log().IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Warning))
								{{
									global::Microsoft.Extensions.Logging.LoggerExtensions.Log(
										__Reactive_Log(),
										global::Microsoft.Extensions.Logging.LogLevel.Warning,
										$""Failed to initialize '{member.Name}' from the updated model, this member is unlikely to work properly."");
								}}
							}}").Align(6)}

						{(hasBaseType ? "" : @$"
							if ({N.Ctor.Model} is global::System.ComponentModel.INotifyPropertyChanged npc)
							{{
								npc.PropertyChanged += __Reactive_OnModelPropertyChanged;
							}}").Align(6)}
					}}
					#endregion

					private void __Reactive_OnModelPropertyChanged(object? sender, global::System.ComponentModel.PropertyChangedEventArgs args)
						=> base.RaisePropertyChanged(args.PropertyName);

					{hasBaseType switch {
						false => $"public {model.ToFullString()} {N.Model} {{ get; private set; }}",
						true => $"public new {model.ToFullString()} {N.Model} => ({model.ToFullString()}) base.{N.Model};",
					}}

					{members.Select(member => member.GetDeclaration()).Align(5)}
				}}");


		// If type is at least internally accessible, add it to a mapping from the VM type to it's bindable counterpart to ease usage in navigation engine.
		// (Private types are almost only a test case which is not supported by nav anyway)
		if (model.DeclaredAccessibility is not Accessibility.Private
			&& model.GetContainingTypes().All(type => type.DeclaredAccessibility is not Accessibility.Private))
		{
			_viewModelsMapping.Register(model, GetViewModelFullName(model));
		}

		return vm.Align(0);
	}

	/// <summary>
	/// Gets the instance of the 
	/// </summary>
	/// <param name="model"></param>
	/// <returns></returns>
	private string GeneratePartialModel(INamedTypeSymbol model)
	{
		var vm = GetViewModelFullName(model);
		return this.AsPartialOf(
			model,
			attributes: $"[{NS.Bindings}.Model(typeof({vm}))]\r\n[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]",
			bases: $"global::System.IAsyncDisposable, {NS.Core}.ISourceContextAware, {NS.Bindings}.IModel<{vm}>",
			code: $@"
				[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
				internal {vm} __reactiveBindableViewModel = default!;

				/// <inheritdoc />
				{vm} {NS.Bindings}.IModel<{vm}>.ViewModel => __reactiveBindableViewModel;

				/// <inheritdoc />
				{this.GetCodeGenAttribute()}
				public global::System.Threading.Tasks.ValueTask DisposeAsync()
					=> {NS.Core}.SourceContext.Find(this)?.DisposeAsync() ?? default;
			");
	}

	private string GetCtorAccessibility(IMethodSymbol ctor)
		=> ctor.DeclaredAccessibility == Accessibility.Private
			? "public"
			: ctor.GetAccessibilityAsCSharpCodeString();

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
