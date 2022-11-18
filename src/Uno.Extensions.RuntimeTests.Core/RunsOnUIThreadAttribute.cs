using System;
using System.Linq;

namespace Uno.UI.RuntimeTests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RunsOnUIThreadAttribute : Attribute { }
