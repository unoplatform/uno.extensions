using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A VM trigger parameter that can be converted into a Command.
/// </summary>
internal record CommandInput(IParameterSymbol Parameter, ITypeSymbol? _commandParameterType) : IInputInfo
{
	private readonly ITypeSymbol? _commandParameterType = _commandParameterType;

	/// <inheritdoc />
	public IParameterSymbol Parameter { get; } = Parameter;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public (string? code, bool isOptional) GetCtorParameter()
		=> (null, true);

	/// <inheritdoc />
	public string? GetCtorInit(bool isInVmCtorParameters)
		=> $"var {Parameter.Name}Builder = new {NS.Commands}.CommandBuilder<{_commandParameterType?.ToString() ?? "object?"}>(nameof({Parameter.GetPascalCaseName()}));";

	/// <inheritdoc />
	public string GetVMCtorParameter()
		=> $"{Parameter.Name}Builder as {Parameter.Type}";

	/// <inheritdoc />
	public string? GetPropertyInit()
		=> $"{Parameter.GetPascalCaseName()} = {Parameter.Name}Builder.Build({N.Ctor.Ctx});";

	// <inheritdoc />
	public Property Property => new(Accessibility.Public, $"{NS.Reactive}.IAsyncCommand", Parameter.GetPascalCaseName()) { HasGetter = true };

	/// <inheritdoc />
	public virtual bool Equals(IInputInfo other)
		=> other is CommandInput otherCommand
			&& Parameter.Name.Equals(otherCommand.Parameter.Name, StringComparison.OrdinalIgnoreCase)
			&& SymbolEqualityComparer.Default.Equals(_commandParameterType, otherCommand._commandParameterType);
}
