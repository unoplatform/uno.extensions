using System;
using System.Collections.Generic;
using System.Text;
using GeneratedSerializers;
using Uno;

namespace ApplicationTemplate.Client
{
    [GeneratedImmutable]
    public partial class PostData
    {
        [EqualityKey]
        public long Id { get; }

        public string Title { get; }

        public string Body { get; }

        [SerializationProperty("UserId")]
        public long UserIdentifier { get; }

        public bool Exists => Id != 0;

        public override string ToString()
        {
            return $"[Id={Id}, Title={Title}, Body={Body}, UserIdentifier={UserIdentifier}]";
        }
    }
}
