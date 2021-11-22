using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Uno.Extensions.Reactive;

namespace ReactiveSample
{
	public partial class ViewModel
	{
		private int _counter;

		private IFeed<long> Seconds => Feed.AsyncEnumerable(() => BuilderTimer());

		public IFeed<Color> Color => Seconds
			.Where(t => t > 4)
			.SelectAsync((_, ct) => Increment(ct))
			.Select(i => i % 2 == 0 ? Colors.Red : Colors.Green);

		private async IAsyncEnumerable<long> BuilderTimer([EnumeratorCancellation] CancellationToken ct = default)
		{
			var time = 0;
			while (!ct.IsCancellationRequested)
			{
				yield return time++;
				await Task.Delay(3000, ct);
			}
		}

		private async ValueTask<int> Increment(CancellationToken ct)
		{
			await Task.Delay(1500, ct);

			if (++_counter % 10 == 0)
			{
				throw new InvalidOperationException("Oups");
			}

			return _counter;
		}
	}
}
