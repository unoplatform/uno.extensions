using System;
using System.Linq;

namespace Uno.Extensions.Edition;

/// <summary>
/// Flags the default constructor to use to create an instance of a record that is being edited.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public class DefaultConstructorAttribute : Attribute
{
}
