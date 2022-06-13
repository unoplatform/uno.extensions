using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	internal class TestObservableVector : List<object?>, IObservableVector<object?>
	{
		public event VectorChangedEventHandler<object?> VectorChanged
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}
	}
}
