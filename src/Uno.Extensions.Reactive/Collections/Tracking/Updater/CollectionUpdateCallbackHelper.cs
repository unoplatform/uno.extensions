using System;
using System.Collections.Generic;

namespace Uno.Extensions.Collections.Tracking;

internal static class CollectionUpdateCallbackHelper
{
	public static BeforeCallback? Combine(this List<BeforeCallback> uiCallbacks)
	{
		switch (uiCallbacks.Count)
		{
			case 0:
				return null;
			case 1:
				return uiCallbacks[0];
			default:
				return (BeforeCallback?)Delegate.Combine(uiCallbacks.ToArray())!;
		}
	}

	public static BeforeCallback? Combine(this BeforeCallback[] uiCallbacks)
	{
		switch (uiCallbacks.Length)
		{
			case 0:
				return null;
			case 1:
				return uiCallbacks[0];
			default:
				return (BeforeCallback?)Delegate.Combine(uiCallbacks)!;
		}
	}

	public static void InvokeAll(this IEnumerable<BeforeCallback> uiCallbacks)
	{
		foreach (var before in uiCallbacks)
		{
			before();
		}
	}

	public static AfterCallback? Combine(this List<AfterCallback> uiCallbacks)
	{
		switch (uiCallbacks.Count)
		{
			case 0:
				return null;
			case 1:
				return uiCallbacks[0];
			default:
				return (AfterCallback?)Delegate.Combine(uiCallbacks.ToArray());
		}
	}

	public static AfterCallback? Combine(this AfterCallback[] uiCallbacks)
	{
		switch (uiCallbacks.Length)
		{
			case 0:
				return null;
			case 1:
				return uiCallbacks[0];
			default:
				return (AfterCallback?)Delegate.Combine(uiCallbacks);
		}
	}

	public static void InvokeAll(this IEnumerable<AfterCallback> uiCallbacks)
	{
		foreach (var after in uiCallbacks)
		{
			after();
		}
	}
}
