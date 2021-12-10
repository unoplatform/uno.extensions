using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// Info about an input requested by a user's VM (i.e. a ctor parameter)
/// </summary>
internal interface IInputInfo : IEquatable<IInputInfo>
{
	public IParameterSymbol Parameter { get; }

	Property? Property { get; }

	string? GetBackingField();

	(string? code, bool isOptional) GetCtorParameter();

	string GetVMCtorParameter();

	string? GetCtorInit(bool isInVmCtorParameters);

	string? GetPropertyInit();
}
