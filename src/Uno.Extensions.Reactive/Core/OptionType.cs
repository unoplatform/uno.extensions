using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public enum OptionType : short
{
	None = 0, // The default so default(Option<T>) == Option<T>.None

	Some = 1,

	Undefined = -1,
}
