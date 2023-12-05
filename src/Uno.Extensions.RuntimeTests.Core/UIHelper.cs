using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests;

public static partial class UIHelper
{
	public static TimeSpan DefaultTimeout => Debugger.IsAttached ? TimeSpan.FromMinutes(60) : TimeSpan.FromSeconds(1);


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
