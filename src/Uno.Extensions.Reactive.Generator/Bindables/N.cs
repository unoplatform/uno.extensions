using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Generator;

internal static class N
{
	/// <summary>
	/// Names of variable while in the generated ctor of a BindableViewModel
	/// </summary>
	public static class Ctor
	{
		public const string Ctx = "ctx";
		public const string Model = "model";
	}

	/// <summary>
	/// Name of the Model property declared in a BindableVM.
	/// </summary>
	public const string Model = "Model";

	public static class ListFeed
	{
		public static class Extensions
		{
			public const string ToListFeed = "global::Uno.Extensions.Reactive.ListFeed.ToListFeed";
		}
	}
}
