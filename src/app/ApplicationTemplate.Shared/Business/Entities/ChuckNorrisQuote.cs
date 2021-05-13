using System;
using System.Collections.Generic;
using System.Text;
using ApplicationTemplate.Client;
using Uno;

namespace ApplicationTemplate.Business
{
    [GeneratedImmutable]
    public partial class ChuckNorrisQuote
    {
        public ChuckNorrisQuote(ChuckNorrisData data, bool isFavorite)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Id = data.Id;
            Value = data.Value;
            IconUrl = data.IconUrl;
            Categories = data.Categories;
            IsFavorite = isFavorite;
        }

        [EqualityKey]
        [EqualityHash]
        public string Id { get; }

        public string Value { get; }

        public string IconUrl { get; }

        public string[] Categories { get; }

        [EqualityIgnore]
        public bool IsFavorite { get; }
    }
}
