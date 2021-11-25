using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal interface IInputInfo : IEquatable<IInputInfo>
{
	public IParameterSymbol Parameter { get; }

	string? GetBackingField();

	(string? code, bool isOptional) GetCtorParameter();

	string GetVMCtorParameter();

	string? GetCtorInit(bool isInVmCtorParameters);

	string? GetPropertyInit();

	string? GetProperty();
}
