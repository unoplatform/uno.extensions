using System;
using System.Collections.Immutable;
using ApplicationTemplate.Client;
using Uno;
using Uno.Extensions.Http;

namespace ApplicationTemplate.Business
{
    //[GeneratedImmutable]
    public partial class ApplicationSettings
    {
        //[EqualityHash]
        public AuthenticationData AuthenticationData { get; set; }

        public bool IsOnboardingCompleted { get; set; }

        public ImmutableDictionary<string, ChuckNorrisQuote> FavoriteQuotes { get; set; } = ImmutableDictionary<string, ChuckNorrisQuote>.Empty;
    }
}
