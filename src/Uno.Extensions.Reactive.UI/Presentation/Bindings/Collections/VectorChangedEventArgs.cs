using System;
using System.Linq;
using Windows.Foundation.Collections;

namespace nVentive.Umbrella.Collections
{
	/// <summary>
	/// Default implementation of <see cref="IVectorChangedEventArgs"/>.
	/// </summary>
	public class VectorChangedEventArgs : IVectorChangedEventArgs
	{
		public VectorChangedEventArgs(CollectionChange change, uint index)
		{
			CollectionChange = change;
			Index = index;
		}

		/// <inheritdoc />
		public CollectionChange CollectionChange { get; }

		/// <inheritdoc />
		public uint Index { get; }
	}
}