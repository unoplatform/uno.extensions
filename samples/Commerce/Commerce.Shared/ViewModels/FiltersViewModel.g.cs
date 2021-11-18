using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

partial class FiltersViewModel : IAsyncDisposable
{
	public class BindableFiltersViewModel : BindableViewModelBase
	{
		private readonly BindableFilter _filter;

		public BindableFiltersViewModel(
			Filters? defaultFilter = default)
		{
			_filter = new BindableFilter(Property(nameof(Filter), defaultFilter, out var filterSubject));

			var vm = new FiltersViewModel(filterSubject);
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
		}

		public FiltersViewModel Model { get; }

		public BindableFilter Filter => _filter;
	}

	public class BindableFilter : Bindable<Filters>
	{
		private readonly Bindable<bool> _shoes;
		private readonly Bindable<bool> _accessories;
		private readonly Bindable<bool> _headwear;

		public BindableFilter(BindablePropertyInfo<Filters> property)
			: base(property, true)
		{
			_shoes = new Bindable<bool>(Property<bool>(nameof(Shoes), p => p?.Shoes ?? default(bool), (p, shoes) => (p ?? new()) with { Shoes = shoes }));
			_accessories = new Bindable<bool>(Property<bool>(nameof(Accessories), p => p?.Accessories ?? default(bool), (p, accessories) => (p ?? new()) with { Accessories = accessories }));
			_headwear = new Bindable<bool>(Property<bool>(nameof(Headwear), p => p?.Headwear ?? default(bool), (p, headwear) => (p ?? new()) with { Headwear = headwear }));
		}

		public Filters Value => GetValue();

		public bool Shoes
		{
			get => _shoes.GetValue();
			set => _shoes.SetValue(value);
		}

		public bool Accessories
		{
			get => _accessories.GetValue();
			set => _accessories.SetValue(value);
		}

		public bool Headwear
		{
			get => _headwear.GetValue();
			set => _headwear.SetValue(value);
		}
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
