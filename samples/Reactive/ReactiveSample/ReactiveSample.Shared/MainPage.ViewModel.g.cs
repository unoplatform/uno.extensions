using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Uno.Extensions.Reactive;

namespace ReactiveSample;

public partial class ViewModel : IAsyncDisposable
{
	public class BindableViewModel : BindableViewModelBase
	{
		public BindableViewModel()
		{
			var vm = new ViewModel();
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
			Color = ctx.GetOrCreateState(vm.Color);
		}

		public ViewModel Model { get; }

		public IFeed<Color> Color { get; }
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
