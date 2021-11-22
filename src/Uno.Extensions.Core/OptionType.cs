#pragma warning disable CS1591 // XML Doc, will be moved elsewhere

using System;
using System.Linq;

namespace Uno.Extensions;

public enum OptionType : short
{
	None = 0, // The default so default(Option<T>) == Option<T>.None

	Some = 1,

	Undefined = -1,
}
