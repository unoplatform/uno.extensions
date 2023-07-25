using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace UnoMusicApp.Helpers
{
	/// <summary>
	/// 
	/// </summary>
	public static class ThreadHelpers
	{
		static bool IsMainThread => synchronizationContext == SynchronizationContext.Current;

		internal static bool WhatThreadAmI([CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
		{
			Debug.WriteLine("**************");
			Debug.WriteLine("**************");
			Debug.WriteLine($"{method}, line: {line}, IsMainThread: {IsMainThread}");
			Debug.WriteLine("**************");
			Debug.WriteLine("**************");
			return IsMainThread;
		}

		static SynchronizationContext? synchronizationContext;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="UiContext"></param>
		public static void Init(SynchronizationContext UiContext) => (synchronizationContext) = (UiContext);

		internal static void BeginInvokeOnMainThread(Action action)
		{
			if (synchronizationContext is not null && SynchronizationContext.Current != synchronizationContext)
				synchronizationContext.Post(_ => action(), null);
			else
				action();
		}
	}
}
