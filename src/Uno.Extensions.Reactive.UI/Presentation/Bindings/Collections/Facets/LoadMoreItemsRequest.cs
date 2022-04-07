using System;
using System.Linq;
using System.Threading.Tasks;
using Umbrella.Feeds.Collections.Extensions;

namespace Umbrella.Presentation.Feeds.Collections
{
	public class LoadMoreItemsRequest
	{
		private readonly TaskCompletionSource<IPageContent> _result = new TaskCompletionSource<IPageContent>();

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
				return (await _result.Task).Count;
			}
			catch
			{
				return 0;
			}
		}
	}
}
