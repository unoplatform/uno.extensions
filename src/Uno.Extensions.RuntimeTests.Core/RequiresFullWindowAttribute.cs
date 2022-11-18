using System;
using System.Linq;

namespace Uno.UI.RuntimeTests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresFullWindowAttribute : Attribute { }
