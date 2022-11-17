using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Indicates that the input should be considered as a simple value. It can be get/set through bindings, but it won't be de-normalized. 
/// </summary>
/// <remarks>This should be used for records inputs for which we want to disable the default de-normalization.</remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class ValueAttribute : Attribute { }
