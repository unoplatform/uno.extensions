using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uno.Extensions.Collections;

namespace Uno.Extensions.Reactive
{
	/// <summary>
	/// A set of <see cref="IChange"/>.
	/// </summary>
	public interface IChangeSet : IEnumerable<IChange>
	{
	}

	/// <summary>
	/// Describes a change that occurred between 2 instances of the same object.
	/// </summary>
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
