using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Uno.Extensions.Reactive;

public static class MessageAxisExtensions
{
	#region Data
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Data instead
	public static Option<object> GetData(this IMessageEntry entry)
		=> MessageAxis.Data.FromMessageValue(entry[MessageAxis.Data]);

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Data instead
	public static Option<T> GetData<T>(this IMessageEntry entry)
		=> MessageAxis.Data.FromMessageValue<T>(entry[MessageAxis.Data]);

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Data instead
	public static Option<T> GetData<T>(this MessageEntry<T> entry)
		=> MessageAxis.Data.FromMessageValue<T>(entry[MessageAxis.Data]);

	public static TBuilder Data<TBuilder, T>(this TBuilder builder, T value)
		where TBuilder : IMessageBuilder<T>
		=> builder.Data((Option<T>)value);

	public static TBuilder Data<TBuilder, T>(this TBuilder builder, Option<T> data)
		where TBuilder : IMessageBuilder<T>
	{
		builder[MessageAxis.Data] = MessageAxis.Data.ToMessageValue(data);

		return builder;
	}
	#endregion

	#region Generic
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static T? Get<TBuilder, T>(this TBuilder builder, MessageAxis<T> axis)
		where TBuilder : IMessageBuilder
		=> axis.FromMessageValue(builder[axis]);

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static T? Get<T>(this IMessageEntry entry, MessageAxis<T> axis)
		=> axis.FromMessageValue(entry[axis]);

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static TBuilder Set<TBuilder, T>(this TBuilder builder, MessageAxis<T> axis, T? value)
		where TBuilder : IMessageBuilder
	{
		builder[axis] = axis.ToMessageValue(value);
		return builder;
	}
	#endregion

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Error instead
	public static Exception? GetError(this IMessageEntry entry)
		=> entry.Get(MessageAxis.Error);

	public static TBuilder Error<TBuilder>(this TBuilder builder, Exception? error)
		where TBuilder : IMessageBuilder
		=> builder.Set(MessageAxis.Error, error);

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Progress instead
	public static bool GetProgress(this IMessageEntry entry)
		=> MessageAxis.Progress.FromMessageValue(entry[MessageAxis.Progress]);

	public static TBuilder IsTransient<TBuilder>(this TBuilder builder, bool isTransient)
		where TBuilder : IMessageBuilder
	{
		builder[MessageAxis.Progress] = MessageAxis.Progress.ToMessageValue(isTransient);

		return builder;
	}
}
