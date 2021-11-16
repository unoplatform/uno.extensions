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
		private readonly Bindable<string> _searchTerm;
		//private readonly Bindable<Filter> _filter;
		private  BindableFilter _filter;

		public BindableProductsViewModel(
			IProductService products,
			string? defaultSearchTerm = default,
			Filters? defaultFilter = default)
		{
			_searchTerm = new Bindable<string>(Property(nameof(SearchTerm), defaultSearchTerm, out var searchTermSubject));
			//_filter = new Bindable<Filter>(Property(nameof(Filter), defaultFilter ?? new(), out var filterSubject));
			_filter = new BindableFilter(Property(nameof(Filter), defaultFilter, out var filterSubject));

			var vm = new ProductsViewModel(products, searchTermSubject, filterSubject);
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

		//public Filter Filter
		//{
		//	get => _filterQuery.GetValue();
		//	set => _filterQuery.SetValue(value);
		//}

		public Filters Filter
		{
			get => _filter.GetValue();
			set => _filter.SetValue(value);
		}
	}

	//public class BindableFilter : Bindable<Filter>
	//{
	//	private readonly Bindable<bool?> _shoes;
	//	private readonly Bindable<bool?> _accessories;
	//	private readonly Bindable<bool?> _headwear;

	//	public BindableFilter(BindablePropertyInfo<Filter> property)
	//		: base(property)
	//	{
	//		_shoes = new Bindable<bool?>(Property<bool?>(nameof(Shoes), p => p?.Shoes, (p, shoes) => (p ?? new()) with { Shoes = shoes ?? default(bool) }));
	//		_accessories = new Bindable<bool?>(Property<bool?>(nameof(Accessories), p => p?.Accessories, (p, accessories) => (p ?? new()) with { Accessories = accessories ?? default(bool) }));
	//		_headwear = new Bindable<bool?>(Property<bool?>(nameof(Headwear), p => p?.Headwear, (p, headwear) => (p ?? new()) with { Headwear = headwear ?? default(bool) }));
	//	}

	//	public bool? Shoes
	//	{
	//		get => _shoes.GetValue();
	//		set => _shoes.SetValue(value);
	//	}

	//	public bool? Accessories
	//	{
	//		get => _accessories.GetValue();
	//		set => _accessories.SetValue(value);
	//	}

	//	public bool? Headwear
	//	{
	//		get => _headwear.GetValue();
	//		set => _headwear.SetValue(value);
	//	}
	//}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
