using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of <see cref="Message{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the value of the message to build.</typeparam>
public readonly struct MessageBuilder<T> : IMessageEntry, IMessageEntry<T>, IMessageBuilder, IMessageBuilder<T>
{
	// We prefer to use this class over the interface for public API in order to hide the Get and Set of the interface,
	// as they should be used only for extensibility, but not in application code.
	// Note: We should consider to make this an abstract base class, but it would cause breaking change
	//		 and significant changes in the code that are not the purpose of the current work.

	internal delegate (MessageAxisValue value, IChangeSet? changes) Getter(MessageAxis axis);
	internal delegate void Setter(MessageAxis axis, MessageAxisValue value, IChangeSet? changes);

	private readonly Getter _get;
	private readonly Setter _set;
	private readonly Func<Message<T>>? _build;

	internal MessageBuilder(MessageEntry<T> current)
	{
		// We make sure to clear all transient axes when we update a message
		var values = current.Values.ToDictionaryWhereKey(k => !k.IsTransient);
		var changeCollection = new ChangeCollection();

		_get = DoGet;
		_set = DoSet;
		_build = DoBuild;

		(MessageAxisValue value, IChangeSet? changes) DoGet(MessageAxis axis)
			=> (values.TryGetValue(axis, out var value) ? value : default, default);

		void DoSet(MessageAxis axis, MessageAxisValue value, IChangeSet? changes)
		{
			var current = DoGet(axis);
			if (axis.AreEquals(current.value, value))
			{
				return;
			}

			if (value.IsSet)
			{
				values[axis] = value;
				changeCollection.Set(axis, changes);
			}
			else if (values.Remove(axis))
			{
				changeCollection.Set(axis, changes);
			}
		}

		Message<T> DoBuild()
			=> new(current, new MessageEntry<T>(values), changeCollection);
	}

	/// <summary>
	/// Creates builder that wraps another IMessageBuilder
	/// </summary>
	/// <param name="get">The get method.</param>
	/// <param name="set">The set method.</param>
	internal MessageBuilder(Getter get, Setter set)
	{
		_get = get;
		_set = set;
		_build = null;
	}

	#region IMessageEntry
	Option<object> IMessageEntry.Data => CurrentData;
	Option<T> IMessageEntry<T>.Data => CurrentData;
	Exception? IMessageEntry.Error => CurrentError;
	bool IMessageEntry.IsTransient => CurrentIsTransient;
	MessageAxisValue IMessageEntry.this[MessageAxis axis] => Get(axis).value;
	IEnumerator IEnumerable.GetEnumerator()
		=> ((IEnumerable)Build().Current).GetEnumerator();
	IEnumerator<KeyValuePair<MessageAxis, MessageAxisValue>> IEnumerable<KeyValuePair<MessageAxis, MessageAxisValue>>.GetEnumerator()
		=> ((IEnumerable<KeyValuePair<MessageAxis, MessageAxisValue>>)Build().Current).GetEnumerator();
	#endregion

	/// <summary>
	/// Gets the current data of the message build by this builder.
	/// </summary>
	/// <remarks>
	/// This represents the current value, if the data has already been modified on this builder,
	/// the value returned will reflect that change.
	/// This means that this value is only a syntax sugar but **MUST NOT be used as reference for change tracking**.
	/// </remarks>
	public Option<T> CurrentData => this.GetData<T>();

	/// <summary>
	/// Gets the current error of the message build by this builder.
	/// </summary>
	/// <remarks>
	/// This represents the current value, if the error has already been modified on this builder,
	/// the value returned will reflect that change.
	/// This means that this value is only a syntax sugar but **MUST NOT be used as reference for change tracking**.
	/// </remarks>
	public Exception? CurrentError => this.GetError();


	/// <summary>
	/// Gets the current progress of the message build by this builder.
	/// </summary>
	/// <remarks>
	/// This represents the current value, if the error has already been modified on this builder,
	/// the value returned will reflect that change.
	/// This means that this value is only a syntax sugar but **MUST NOT be used as reference for change tracking**.
	/// </remarks>
	public bool CurrentIsTransient => this.GetProgress();

	(MessageAxisValue value, IChangeSet? changes) IMessageBuilder.Get(MessageAxis axis)
		=> Get(axis);
	internal (MessageAxisValue value, IChangeSet? changes) Get(MessageAxis axis)
		=> _get(axis);

	void IMessageBuilder.Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes)
		=> Set(axis, value, changes);

	internal MessageBuilder<T> Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes)
	{
		_set(axis, value, changes);
		return this;
	}

	/// <summary>
	/// Builds the resulting message.
	/// </summary>
	public Message<T> Build()
		=> _build is null
			? throw new InvalidOperationException("This builder does not support direct convert to Message<T>.")
			: _build();

	/// <summary>
	/// Builds the resulting message.
	/// </summary>
	/// <param name="builder">The builder to build.</param>
	public static implicit operator Message<T>(MessageBuilder<T> builder)
		=> builder.Build();
}
