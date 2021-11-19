#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;
using static Commerce.ViewModels.FiltersViewModel;

namespace Commerce.ViewModels;

public partial class ProductsViewModel : IAsyncDisposable
{
	public class BindableProductsViewModel : BindableViewModelBase
	{
		private readonly Bindable<string> _term;
		private readonly Bindable<Filters> _filter;

		public BindableProductsViewModel(
			IProductService products,
			string? defaultSearchTerm = default,
			Filters? defaultFilter = default)
		{
			_term = new Bindable<string>(Property(nameof(Term), defaultSearchTerm, out var termSubject));
			_filter = new Bindable<Filters>(Property(nameof(Filter), defaultFilter ?? new(), out var filterSubject));

			var vm = new ProductsViewModel(products, termSubject, filterSubject);
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
			Items = ctx.GetOrCreateState(vm.Items);
		}

		public ProductsViewModel Model { get; }

		public IFeed<Product[]> Items { get; }

		public string Term
		{
			get => _term.GetValue();
			set => _term.SetValue(value);
		}

		public Filters Filter
		{
			get => _filter.GetValue();
			set => _filter.SetValue(value);
		}
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
