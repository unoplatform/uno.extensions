using System;
using System.Linq;
using System.Threading.Tasks;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial class ProductsViewModel : IAsyncDisposable
{
	public class BindableProductsViewModel : BindableViewModelBase
	{
		private readonly Bindable<string> _searchTerm;
		private readonly Bindable<string> _filterQuery;

		public BindableProductsViewModel(
			IProductService products,
			string? defaultSearchTerm = "",
			string? defaultFilterQuery = "")
		{
			_searchTerm = new Bindable<string>(Property(nameof(SearchTerm), defaultSearchTerm, out var searchTermSubject));
			_filterQuery = new Bindable<string>(Property(nameof(FilterQuery), defaultFilterQuery, out var filterQuerySubject));

			var vm = new ProductsViewModel(products, searchTermSubject, filterQuerySubject);
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
			Items = ctx.GetOrCreateState(vm.Items);
		}

		public ProductsViewModel Model { get; }

		public IFeed<Product[]> Items { get; }

		public string SearchTerm
		{
			get => _searchTerm.GetValue();
			set => _searchTerm.SetValue(value);
		}

		public string FilterQuery
		{
			get => _filterQuery.GetValue();
			set => _filterQuery.SetValue(value);
		}
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
