using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Generator;

internal static class NS
{
	public const string Reactive = "global::Uno.Extensions.Reactive";
}

internal static class N
{
	/// <summary>
	/// Names of variable while in the generated ctor of a BindableViewModel
	/// </summary>
	public static class Ctor
	{
		public const string Ctx = "ctx";
		public const string Model = "vm";
	}


	public const string Model = "Model";
}
