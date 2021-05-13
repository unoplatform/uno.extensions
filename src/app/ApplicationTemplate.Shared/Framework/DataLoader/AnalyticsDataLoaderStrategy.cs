using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno;

namespace Chinook.DataLoader
{
    /// <summary>
    /// This class is a <see cref="DelegatingDataLoaderStrategy"/> that offers callbacks on success and on error, ideal for analytics.
    /// This class demontrates how easy it is to extend the DataLoader recipe.
    /// </summary>
    public class AnalyticsDataLoaderStrategy : DelegatingDataLoaderStrategy
    {
        private readonly ActionAsync<IDataLoaderRequest, object> _onSuccess;
        private readonly ActionAsync<IDataLoaderRequest, Exception> _onError;

        public AnalyticsDataLoaderStrategy(ActionAsync<IDataLoaderRequest, object> onSuccess, ActionAsync<IDataLoaderRequest, Exception> onError)
        {
            _onSuccess = onSuccess;
            _onError = onError;
        }

        public override async Task<object> Load(CancellationToken ct, IDataLoaderRequest request)
        {
            try
            {
                var result = await base.Load(ct, request);

                await _onSuccess(ct, request, result);

                return result;
            }
            catch (Exception error)
            {
                await _onError(ct, request, error);

                throw;
            }
        }
    }

    public static class AnalyticsDataLoaderStrategyExtensions
    {
        /// <summary>
        /// Adds a <see cref="AnalyticsDataLoaderStrategy"/> to this builder.
        /// </summary>
        /// <typeparam name="TBuilder">The type of the builder.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="onSuccess">The callback when the strategy loads successfully.</param>
        /// <param name="onError">The callback when the strategy fails to load.</param>
        /// <returns>The original builder.</returns>
        public static TBuilder WithAnalytics<TBuilder>(this TBuilder builder, ActionAsync<IDataLoaderRequest, object> onSuccess, ActionAsync<IDataLoaderRequest, Exception> onError)
            where TBuilder : IDataLoaderBuilder
        {
            return builder.WithStrategy(new AnalyticsDataLoaderStrategy(onSuccess, onError));
        }
    }
}
