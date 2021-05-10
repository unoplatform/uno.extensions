using System;
using System.Collections.Generic;
using System.Text;
using GeneratedSerializers;
using Uno;

namespace ApplicationTemplate.Client
{
	[GeneratedImmutable]
	public partial class ChuckNorrisData
	{
		[EqualityKey]
		public string Id { get; }

		public string Value { get; }

		[SerializationProperty("icon_url")]
		public string IconUrl { get; }

		public string[] Categories { get; }
	}
}
