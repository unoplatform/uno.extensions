using System;
using System.Collections.Immutable;
using ApplicationTemplate.Client;
using Uno;

namespace ApplicationTemplate.Business
{
    [GeneratedImmutable]
    public partial class ApplicationSettings
    {
        [EqualityHash]
        public AuthenticationData AuthenticationData { get; }

        public bool IsOnboardingCompleted { get; }

        public ImmutableDictionary<string, ChuckNorrisQuote> FavoriteQuotes { get; } = ImmutableDictionary<string, ChuckNorrisQuote>.Empty;
    }
}
