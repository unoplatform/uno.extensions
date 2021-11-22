using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions;

public interface IOption
{
	OptionType Type { get; }

	object SomeOrDefault();

	bool IsUndefined();

	bool IsNone();

	bool IsSome([NotNullWhen(true)] out object? value);
}
