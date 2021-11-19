#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

partial class ProductDetailsViewModel : IAsyncDisposable
{
	public class BindableProductDetailsViewModel : BindableViewModelBase
	{
		public BindableProductDetailsViewModel(
			IProductService productService,
			Product product)
		{
			var vm = new ProductDetailsViewModel(productService, product);
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
			Product = ctx.GetOrCreateState(vm.Product);
			Reviews = ctx.GetOrCreateState(vm.Reviews);
		}

		public ProductDetailsViewModel Model { get; }

		public IFeed<Product> Product { get; }
		public IFeed<Review[]> Reviews { get; }
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
