using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

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
	public static MessageBuilder<T> Data<T>(this MessageBuilder<T> builder, T value)
		=> builder.Data((Option<T>)value);

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static MessageBuilder<TParent, TResult> Data<TParent, TResult>(this MessageBuilder<TParent, TResult> builder, TResult value)
		=> builder.Data((Option<TResult>)value);

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static MessageBuilder<IImmutableList<T>> Data<T>(this MessageBuilder<IImmutableList<T>> builder, ImmutableList<T> value)
		=> builder.Data((Option<IImmutableList<T>>)value);

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static MessageBuilder<TParent, IImmutableList<TResult>> Data<TParent, TResult>(this MessageBuilder<TParent, IImmutableList<TResult>> builder, ImmutableList<TResult> value)
		=> builder.Data((Option<IImmutableList<TResult>>)value);

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="data">The data to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static MessageBuilder<T> Data<T>(this MessageBuilder<T> builder, Option<T> data)
	{
		builder.Set(MessageAxis.Data, MessageAxis.Data.ToMessageValue(data), null);

		return builder;
	}

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="data">The data to set.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	public static MessageBuilder<TParent, TResult> Data<TParent, TResult>(this MessageBuilder<TParent, TResult> builder, Option<TResult> data)
	{
		builder.Set(MessageAxis.Data, MessageAxis.Data.ToMessageValue(data), null);

		return builder;
	}

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="data">The data to set.</param>
	/// <param name="changeSet">The changes made from the previous value</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	internal static MessageBuilder<T> Data<T>(this MessageBuilder<T> builder, Option<T> data, IChangeSet? changeSet)
	{
		builder.Set(MessageAxis.Data, MessageAxis.Data.ToMessageValue(data), changeSet);

		return builder;
	}

	/// <summary>
	/// Sets the data of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="data">The data to set.</param>
	/// <param name="changeSet">The changes made from the previous value</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	internal static MessageBuilder<TParent, TResult> Data<TParent, TResult>(this MessageBuilder<TParent, TResult> builder, Option<TResult> data, IChangeSet? changeSet)
	{
		builder.Set(MessageAxis.Data, MessageAxis.Data.ToMessageValue(data), changeSet);

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
		=> axis.FromMessageValue(builder.Get(axis).value);

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
		builder.Set(axis, axis.ToMessageValue(value));
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
		builder.Set(MessageAxis.Progress, MessageAxis.Progress.ToMessageValue(isTransient));

		return builder;
	}

	/// <summary>
	/// Sets the refresh info of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="tokens">The refresh tokens.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	internal static TBuilder Refreshed<TBuilder>(this TBuilder builder, TokenSet<RefreshToken>? tokens)
		where TBuilder : IMessageBuilder
		=> builder.Set(MessageAxis.Refresh, tokens);

	/// <summary>
	/// Gets the pagination info of an <see cref="MessageEntry{T}"/>
	/// </summary>
	/// <param name="entry">The entry.</param>
	/// <returns>The pagination info.</returns>
	[Pure]
	internal static PaginationInfo? GetPaginationInfo(this IMessageEntry entry)
		=> entry.Get(MessageAxis.Pagination);

	/// <summary>
	/// Sets the pagination info of an <see cref="MessageBuilder{T}"/>
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="page">The pagination info.</param>
	/// <returns>The <paramref name="builder"/> for fluent building.</returns>
	internal static TBuilder Paginated<TBuilder>(this TBuilder builder, PaginationInfo? page)
		where TBuilder : IMessageBuilder
		=> builder.Set(MessageAxis.Pagination, page);


	/// <summary>
	/// Fluently applies an additional configuration action on a message builder.
	/// </summary>
	/// <typeparam name="TBuilder">Type of the builder to configure.</typeparam>
	/// <param name="builder">The builder to configure.</param>
	/// <param name="configure">The addition configure operation to apply on the builder.</param>
	/// <returns></returns>
	internal static TBuilder Apply<TBuilder>(this TBuilder builder, Action<TBuilder>? configure)
		where TBuilder : IMessageBuilder
	{
		configure?.Invoke(builder);
		return builder;
	}
}
