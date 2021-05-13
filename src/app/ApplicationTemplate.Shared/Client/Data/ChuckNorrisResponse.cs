using System;
using System.Collections.Generic;
using System.Text;
using GeneratedSerializers;
using Uno;

namespace ApplicationTemplate.Client
{
    [GeneratedImmutable]
    public partial class ChuckNorrisResponse
    {
        [EqualityHash]
        [SerializationProperty("result")]
        public ChuckNorrisData[] Quotes { get; }
    }
}
