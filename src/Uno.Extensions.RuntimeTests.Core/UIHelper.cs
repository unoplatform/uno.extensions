using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests;

public static class UIHelper
{
	public static TimeSpan DefaultTimeout => Debugger.IsAttached ? TimeSpan.FromMinutes(60) : TimeSpan.FromSeconds(1);

	public static UIElement? Content
	{
		get => UnitTestsUIContentHelper.Content;
		set => UnitTestsUIContentHelper.Content = value;
	}

	public static async Task Load(FrameworkElement element, CancellationToken ct)
	{
		Content = element;
		await WaitForLoaded(element, ct);
	}

	public static IEnumerable<T> FindChildren<T>(DependencyObject element)
	{
		if (element is T t)
		{
			yield return t;
		}

		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
		{
			foreach (var child in FindChildren<T>(VisualTreeHelper.GetChild(element, i)))
			{
				yield return child;
			}
		}
	}

	public static async Task WaitForLoaded(FrameworkElement element, CancellationToken ct)
	{
		if (element.IsLoaded)
		{
			return;
		}

		var tcs = new TaskCompletionSource<object?>();
		using var _ = ct.CanBeCanceled ? ct.Register(() => tcs.TrySetCanceled()) : default;
		try
		{
			element.Loaded += OnElementLoaded;

			if (!element.IsLoaded)
			{
				var timeout = Task.Delay(DefaultTimeout, ct);
				if (await Task.WhenAny(tcs.Task, timeout) == timeout)
				{
					throw new TimeoutException($"Failed to load element within {DefaultTimeout}.");
				}
			}
		}
		finally
		{
			element.Loaded -= OnElementLoaded;
		}

		void OnElementLoaded(object sender, RoutedEventArgs e)
		{
			element.Loaded -= OnElementLoaded;
			tcs.TrySetResult(default);
		}
	}

	public static async ValueTask WaitFor(Func<bool> predicate, CancellationToken ct)
		=> await WaitFor(async _ => predicate(), ct);
	

	public static async ValueTask WaitFor(Func<CancellationToken, ValueTask<bool>> predicate, CancellationToken ct)
	{
		using var timeout = new CancellationTokenSource(DefaultTimeout);
		try
		{
			ct = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token).Token;

			var delay = Math.Min(1000, (int)(DefaultTimeout.TotalMilliseconds / 100));
			var steps = DefaultTimeout.TotalMilliseconds / delay;

			for (var i = 0; i < steps; i++)
			{
				ct.ThrowIfCancellationRequested();

				if (await predicate(ct))
				{
					return;
				}

				await Task.Delay(delay, ct);
			}

			throw new TimeoutException();
		}
		catch (OperationCanceledException) when (timeout.IsCancellationRequested)
		{
			throw new TimeoutException();
		}
	}
}
