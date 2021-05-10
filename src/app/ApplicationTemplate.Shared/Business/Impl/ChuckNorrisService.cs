using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;
using DynamicData;
using Uno.Extensions;

namespace ApplicationTemplate.Business
{
	public sealed class ChuckNorrisService : IChuckNorrisService, IDisposable
	{
		private readonly IApplicationSettingsService _applicationSettingsService;
		private readonly IChuckNorrisEndpoint _chuckNorrisEndpoint;
		private SourceList<ChuckNorrisQuote> _favouriteQuotes;

		public ChuckNorrisService(IApplicationSettingsService applicationSettingsService, IChuckNorrisEndpoint chuckNorrisEndpoint)
		{
			_applicationSettingsService = applicationSettingsService ?? throw new ArgumentNullException(nameof(applicationSettingsService));
			_chuckNorrisEndpoint = chuckNorrisEndpoint ?? throw new ArgumentNullException(nameof(chuckNorrisEndpoint));
		}

		public async Task<ChuckNorrisQuote[]> Search(CancellationToken ct, string searchTerm)
		{
			// If the search term does not contain at least 3 characters, the API returns an exception.
			// It must be handle on app side.
			if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3)
			{
				return Array.Empty<ChuckNorrisQuote>();
			}

			var response = await _chuckNorrisEndpoint.Search(ct, searchTerm);

			var settings = await _applicationSettingsService.GetCurrent(ct);

			return response
				.Quotes
				.Safe()
				.Select(d => new ChuckNorrisQuote(d, settings.FavoriteQuotes.ContainsKey(d.Id)))
				.ToArray();
		}

		public async Task<IObservableList<ChuckNorrisQuote>> GetFavorites(CancellationToken ct)
		{
			var source = await GetFavouriteQuotesSource(ct);
			return source.AsObservableList();
		}

		public async Task SetIsFavorite(CancellationToken ct, ChuckNorrisQuote quote, bool isFavorite)
		{
			if (quote is null)
			{
				throw new ArgumentNullException(nameof(quote));
			}

			var settings = await _applicationSettingsService.GetCurrent(ct);

			quote = quote.WithIsFavorite(isFavorite);

			var updatedFavorites = isFavorite && !settings.FavoriteQuotes.ContainsKey(quote.Id)
				? settings.FavoriteQuotes.Add(quote.Id, quote)
				: settings.FavoriteQuotes.Remove(quote.Id);

			await _applicationSettingsService.SetFavoriteQuotes(ct, updatedFavorites);

			var source = await GetFavouriteQuotesSource(ct);

			if (isFavorite)
			{
				if (source.Items.None(q => q.Id == quote.Id))
				{
					source.Add(quote);
				}
			}
			else
			{
				var item = source.Items.FirstOrDefault(q => q.Id == quote.Id);
				source.Remove(item);
			}
		}

		private async Task<SourceList<ChuckNorrisQuote>> GetFavouriteQuotesSource(CancellationToken ct)
		{
			if (_favouriteQuotes == null)
			{
				var settings = await _applicationSettingsService.GetCurrent(ct);

				_favouriteQuotes = new SourceList<ChuckNorrisQuote>();
				_favouriteQuotes.AddRange(settings.FavoriteQuotes.Values);
			}

			return _favouriteQuotes;
		}

		public void Dispose()
		{
			_favouriteQuotes?.Dispose();
		}
	}
}
