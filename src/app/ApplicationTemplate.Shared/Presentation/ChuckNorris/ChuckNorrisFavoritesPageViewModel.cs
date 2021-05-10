using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using Chinook.DataLoader;
using Chinook.DynamicMvvm;
using DynamicData;

namespace ApplicationTemplate.Presentation
{
	public class ChuckNorrisFavoritesPageViewModel : ViewModel
	{
		public IDynamicCommand RefreshQuotes => this.GetCommandFromDataLoaderRefresh(Quotes);

		public IDataLoader Quotes => this.GetDataLoader(LoadQuotes, c => c
			.UpdateOnCollectionChanged()
		);

		public IDynamicCommand ToggleIsFavorite => this.GetCommandFromTask<ChuckNorrisItemViewModel>(async (ct, item) =>
		{
			await this.GetService<IChuckNorrisService>().SetIsFavorite(ct, item.Quote, !item.IsFavorite);

			item.IsFavorite = !item.IsFavorite;
		});

		private async Task<ReadOnlyObservableCollection<ChuckNorrisItemViewModel>> LoadQuotes(CancellationToken ct, IDataLoaderRequest request)
		{
			var quotes = await this.GetService<IChuckNorrisService>().GetFavorites(ct);

			// This is an observable list that will dynamically remove items when we unfavorite. We could use 'quotes.Items' if we don't want the list to remove items directly.
			quotes
				.Connect()
				.Transform(q => this.GetChild(() => new ChuckNorrisItemViewModel(this, q), q.Id))
				.ObserveOn(this.GetService<IDispatcherScheduler>())
				.Bind(out var list)
				.DisposeMany()
				.Subscribe()
				.DisposeWithNextLoad(request);

			return list;
		}
	}
}
