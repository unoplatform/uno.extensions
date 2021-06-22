using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Serialization;
using Uno;
using System.Text.Json.Serialization;

namespace ApplicationTemplate.Client
{
    public partial class PostData
    {
        public long Id { get; }

        public string Title { get; }

        public string Body { get; }

        [JsonPropertyName("UserId")]
        public long UserIdentifier { get; }

        public bool Exists => Id != 0;

        public override string ToString()
        {
            return $"[Id={Id}, Title={Title}, Body={Body}, UserIdentifier={UserIdentifier}]";
        }
    }
}
