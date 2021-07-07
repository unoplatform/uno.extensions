using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ApplicationTemplate;
using ApplicationTemplate.Business;
using CommunityToolkit.Mvvm.Input;
//using Chinook.DataLoader;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;
//using DynamicData;

namespace ApplicationTemplate.Presentation
{
    public class ChuckNorrisSearchPageViewModel : ViewModel
    {
        public string SearchTerm { get; set; }
        //public string SearchTerm
        //{
        //    get => this.Get<string>();
        //    set => this.Set(value);
        //}

        private readonly IChuckNorrisService _chuck;

        public ICommand Load { get; set; }
        public ChuckNorrisSearchPageViewModel(IChuckNorrisService chuckService)
        {
            _chuck = chuckService;
            Load = new RelayCommand(LoadAllQuotes);
        }

        private async void LoadAllQuotes()
        {
            var cancel = new CancellationTokenSource();
            var quotes = await LoadQuotes(cancel.Token);
        }


        //public IDynamicCommand NavigateToFavoriteQuotes => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new ChuckNorrisFavoritesPageViewModel());
        //});

        //public IDynamicCommand RefreshQuotes => this.GetCommandFromDataLoaderRefresh(Quotes);

        //public IDataLoader<ChuckNorrisItemViewModel[]> Quotes => this.GetDataLoader(LoadQuotes, b => b
        //    // Dispose the previous ItemViewModels when Quotes produces new values
        //    .DisposePreviousData()
        //    .TriggerFromObservable(SearchTermChanged, nameof(SearchTermChanged))
        //    .TriggerOnNetworkReconnection()
        //);

        //private IObservable<string> SearchTermChanged => this.GetProperty(x => x.SearchTerm)
        //    .Observe()
        //    .Throttle(TimeSpan.FromMilliseconds(300));

        //public IDynamicCommand ToggleIsFavorite => this.GetCommandFromTask<ChuckNorrisItemViewModel>(async (ct, item) =>
        //{
        //    await this.GetService<IChuckNorrisService>().SetIsFavorite(ct, item.Quote, !item.IsFavorite);
        //});

        //private async Task<ChuckNorrisItemViewModel[]> LoadQuotes(CancellationToken ct, IDataLoaderRequest request)
        //{
        //    await SetupFavoritesUpdate(ct);

        //    // Add the SearchTerm to the IDataLoaderContext to be able to bind it in the empty state.
        //    request.Context["SearchTerm"] = SearchTerm;

        //    var quotes = await this.GetService<IChuckNorrisService>().Search(ct, SearchTerm);

        //    return quotes
        //        .Select(q => this.GetChild(() => new ChuckNorrisItemViewModel(this, q), q.Id))
        //        .ToArray();
        //}

        private async Task<ChuckNorrisItemViewModel[]> LoadQuotes(CancellationToken ct)
        {

            var quotes = await _chuck.Search(ct, SearchTerm);

            return quotes
                .Select(q => new ChuckNorrisItemViewModel(q))
                .ToArray();
        }

        //private async Task SetupFavoritesUpdate(CancellationToken ct)
        //{
        //    const string FavoritesKey = "FavoritesSubscription";

        //    if (!TryGetDisposable(FavoritesKey, out var _))
        //    {
        //        // Get the observable list of favorites.
        //        var favorites = await this.GetService<IChuckNorrisService>().GetFavorites(ct);

        //        // Subscribe to the observable list to update the current items.
        //        var subscription = favorites
        //            .Connect()
        //            .Subscribe(UpdateItemViewModels);

        //        AddDisposable(FavoritesKey, subscription);
        //    }

        //    void UpdateItemViewModels(IChangeSet<ChuckNorrisQuote> changeSet)
        //    {
        //        var quotesVMs = Quotes.State.Data;
        //        if (quotesVMs != null && quotesVMs.Any())
        //        {
        //            var addedItems = changeSet.GetAddedItems();
        //            var removedItems = changeSet.GetRemovedItems();

        //            foreach (var quoteVM in quotesVMs)
        //            {
        //                if (addedItems.Any(a => a.Id == quoteVM.Quote.Id))
        //                {
        //                    quoteVM.IsFavorite = true;
        //                }
        //                if (removedItems.Any(r => r.Id == quoteVM.Quote.Id))
        //                {
        //                    quoteVM.IsFavorite = false;
        //                }
        //            }
        //        }
        //    }
        //}
    }
}
