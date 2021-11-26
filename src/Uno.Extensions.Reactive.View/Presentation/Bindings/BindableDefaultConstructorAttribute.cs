using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Flags the default constructor to use to create an instance of a record that is being de-normalized for bindings.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
internal class BindableDefaultConstructorAttribute : Attribute
{
}
