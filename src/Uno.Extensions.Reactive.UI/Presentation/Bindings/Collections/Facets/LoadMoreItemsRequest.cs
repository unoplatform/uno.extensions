using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Bindings.Collections;

internal class LoadMoreItemsRequest
{
	private readonly TaskCompletionSource<IPageContent> _result = new();

	internal LoadMoreItemsRequest(uint requested)
	{
		Requested = requested;
	}

	public uint Requested { get; }

	internal void Completed(IPageContent pageContent)
		=> _result.TrySetResult(pageContent);

	internal void Failed(Exception error)
		=> _result.TrySetException(error);

	internal void Aborted()
		=> _result.TrySetCanceled();

	/// <summary>
	/// Gets the count of loaded items.
	/// </summary>
	/// <returns>Number of items loaded, or 0 if the request was aborted or if the source failed to load more items.</returns>
	public async Task<uint> GetLoaded()
	{
		try
		{
			return (await _result.Task.ConfigureAwait(false)).Count;
		}
		catch (OperationCanceledException)
		{
			return 0;
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn(error, "Failed to load more items.");
			}

			return 0;
		}
	}
}
