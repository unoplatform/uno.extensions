using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno;

namespace Chinook.DataLoader
{
    /// <summary>
    /// This class is a <see cref="DelegatingDataLoaderStrategy"/> that offers a callback when the previous data reference is lost when getting new data.
    /// </summary>
    public class OnDataLostDataLoaderStrategy : DelegatingDataLoaderStrategy
    {
        private readonly Action<object> _onPreviousDataLost;
        private object _data;

        public OnDataLostDataLoaderStrategy(Action<object> onPreviousDataLost)
        {
            _onPreviousDataLost = onPreviousDataLost;
        }

        public override async Task<object> Load(CancellationToken ct, IDataLoaderRequest request)
        {
            var result = await base.Load(ct, request);

            // We should not dispose the previous data if we load the same instance.
            if (_data != null && !ReferenceEquals(_data, result))
            {
                _onPreviousDataLost(_data);
            }
            _data = result;

            return result;
        }
    }

    public static class OnDataLostDataLoaderStrategyExtensions
    {
        /// <summary>
        /// Adds a <see cref="OnDataLostDataLoaderStrategy"/> to this builder that will dispose the previous data when new data is received.
        /// </summary>
        /// <typeparam name="TBuilder">The type of the builder.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns>The original builder.</returns>
        public static TBuilder DisposePreviousData<TBuilder>(this TBuilder builder)
            where TBuilder : IDataLoaderBuilder
        {
            return builder.WithStrategy(new OnDataLostDataLoaderStrategy(DisposeData));

            void DisposeData(object data)
            {
                if (data is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (data is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        if (item is IDisposable disposableItem)
                        {
                            disposableItem.Dispose();
                        }
                    }
                }
            }
        }
    }
}
