using System;
using System.Linq;

namespace Uno.Extensions.Equality;

/// <summary>
/// Flags a property as the key for entity tracking.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute
{
}
