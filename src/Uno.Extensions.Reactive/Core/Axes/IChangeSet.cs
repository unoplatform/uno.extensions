using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uno.Extensions.Collections;

namespace Uno.Extensions.Reactive
{
	public interface IChangeSet : IEnumerable<IChange>
	{
	}

	//internal interface IChangeSet<out TChange> : IChangeSet, IEnumerable<TChange>
	//	where TChange : IChange
	//{
	//}

	public interface IChange
	{
	}

	internal interface ICollectionChange : IChange
	{
		RichNotifyCollectionChangedEventArgs? ToEvent();
	}

	internal interface IEntityChange : IChange
	{
		PropertyChangedEventArgs? ToEvent();
	}
}
