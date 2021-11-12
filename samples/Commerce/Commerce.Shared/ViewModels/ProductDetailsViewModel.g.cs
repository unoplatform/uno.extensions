using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

partial class ProductDetailsViewModel : IAsyncDisposable
{
	public class BindableProductDetailsViewModel : BindableViewModelBase
	{
		public BindableProductDetailsViewModel(
			IProductService service,
			Product product,
			IDictionary<string, object> parameters)
		{
			var vm = new ProductDetailsViewModel(service, product, parameters);
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
			Product = ctx.GetOrCreateState(vm.Product);
		}

		public ProductDetailsViewModel Model { get; }

		public IFeed<Product> Product { get; }
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
