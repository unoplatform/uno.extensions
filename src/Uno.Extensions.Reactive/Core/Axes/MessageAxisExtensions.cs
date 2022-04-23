using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to manipulate <see cref="MessageEntry{T}"/>.
/// </summary>
public static class MessageAxisExtensions
{
	#region Data
	/// <summary>
	/// Gets the untyped data of an <see cref="MessageEntry{T}"/>
	/// </summary>
	/// <param name="entry">The entry.</param>
	/// <returns>The untyped data.</returns>
	/// <remarks>Use <see cref="MessageEntry{T}.Data"/> instead.</remarks>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Data instead
	public static Option<object> GetData(this IMessageEntry entry)
		=> MessageAxis.Data.FromMessageValue(entry[MessageAxis.Data]);

	/// <summary>
	/// Gets the data of an <see cref="MessageEntry{T}"/>
	/// </summary>
	/// <param name="entry">The entry.</param>
	/// <returns>The data.</returns>
	/// <remarks>Use <see cref="MessageEntry{T}.Data"/> instead.</remarks>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Data instead
	public static Option<T> GetData<T>(this IMessageEntry entry)
		=> MessageAxis.Data.FromMessageValue<T>(entry[MessageAxis.Data]);

	/// <summary>
	/// Gets the data of an <see cref="MessageEntry{T}"/>
	/// </summary>
	/// <param name="entry">The entry.</param>
	/// <returns>The data.</returns>
	/// <remarks>Use <see cref="MessageEntry{T}.Data"/> instead.</remarks>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Data instead
	public static Option<T> GetData<T>(this MessageEntry<T> entry)
		=> MessageAxis.Data.FromMessageValue<T>(entry[MessageAxis.Data]);

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static TBuilder Data<TBuilder, T>(this TBuilder builder, T value)
		where TBuilder : IMessageBuilder<T>
		=> builder.Data((Option<T>)value);

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="changeSet">The changes made from the previous value</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	internal static TBuilder Data<TBuilder, T>(this TBuilder builder, T value, IChangeSet changeSet)
		where TBuilder : IMessageBuilder<T>
		=> builder.Data((Option<T>)value);

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="data">The data to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static TBuilder Data<TBuilder, T>(this TBuilder builder, Option<T> data)
		where TBuilder : IMessageBuilder<T>
	{
		builder[MessageAxis.Data] = MessageAxis.Data.ToMessageValue(data);

		return builder;
	}

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="data">The data to set.</param>
	/// <param name="changeSet">The changes made from the previous value</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	internal static TBuilder Data<TBuilder, T>(this TBuilder builder, Option<T> data, IChangeSet changeSet)
		where TBuilder : IMessageBuilder<T>
	{
		builder[MessageAxis.Data] = MessageAxis.Data.ToMessageValue(data);

		return builder;
	}
	#endregion

	#region Generic
	/// <summary>
	/// Gets a metadata of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="axis">The metadata axis to get.</param>
	/// <returns>The metadata.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)] // Create dedicated extension methods
	public static T? Get<TBuilder, T>(this TBuilder builder, MessageAxis<T> axis)
		where TBuilder : IMessageBuilder
		=> axis.FromMessageValue(builder[axis]);

	/// <summary>
	/// Gets a metadata of an <see cref="MessageEntry{T}"/>
	/// </summary>
	/// <param name="entry">The entry.</param>
	/// <param name="axis">The metadata axis to get.</param>
	/// <returns>The metadata.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static T? Get<T>(this IMessageEntry entry, MessageAxis<T> axis)
		=> axis.FromMessageValue(entry[axis]);

	/// <summary>
	/// Sets a metadata of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="axis">The metadata axis to set.</param>
	/// <param name="value">The value of the metadata to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static TBuilder Set<TBuilder, T>(this TBuilder builder, MessageAxis<T> axis, T? value)
		where TBuilder : IMessageBuilder
	{
		builder[axis] = axis.ToMessageValue(value);
		return builder;
	}
	#endregion

	/// <summary>
	/// Gets the error of an <see cref="MessageEntry{T}"/>
	/// </summary>
	/// <param name="entry">The entry.</param>
	/// <returns>The error if any.</returns>
	/// <remarks>Use <see cref="MessageEntry{T}.Error"/> instead.</remarks>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Error instead
	public static Exception? GetError(this IMessageEntry entry)
		=> entry.Get(MessageAxis.Error);

	/// <summary>
	/// Sets the error of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="error">The error to set, or null to clear error.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static TBuilder Error<TBuilder>(this TBuilder builder, Exception? error)
		where TBuilder : IMessageBuilder
		=> builder.Set(MessageAxis.Error, error);


	/// <summary>
	/// Gets the progress of an <see cref="MessageEntry{T}"/>
	/// </summary>
	/// <param name="entry">The entry.</param>
	/// <returns>The progress.</returns>
	/// <remarks>Use <see cref="MessageEntry{T}.IsTransient"/> instead.</remarks>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use IMessageEntry.Progress instead
	public static bool GetProgress(this IMessageEntry entry)
		=> MessageAxis.Progress.FromMessageValue(entry[MessageAxis.Progress]);

	/// <summary>
	/// Sets the progress of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="isTransient">The progress to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static TBuilder IsTransient<TBuilder>(this TBuilder builder, bool isTransient)
		where TBuilder : IMessageBuilder
	{
		builder[MessageAxis.Progress] = MessageAxis.Progress.ToMessageValue(isTransient);

		return builder;
	}
}
