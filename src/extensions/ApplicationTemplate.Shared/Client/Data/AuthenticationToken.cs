using System;
using GeneratedSerializers;
using Uno;

namespace ApplicationTemplate.Client
{
    [GeneratedImmutable]
    public partial class AuthenticationToken
    {
        [EqualityKey]
        [SerializationProperty("unique_name")]
        public string Email { get; }

        [SerializationProperty("exp")]
        [CustomDeserializer(typeof(UnixTimestampSerializer))]
        public DateTimeOffset Expiration { get; } = DateTimeOffset.MinValue;

        [SerializationProperty("iat")]
        [CustomDeserializer(typeof(UnixTimestampSerializer))]
        public DateTimeOffset IssuedAt { get; } = DateTimeOffset.MinValue;
    }
}
