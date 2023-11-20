using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Uno.Extensions.Collections.Facades.Map;
using Uno.Extensions.Conversion;

namespace Uno.Extensions.Collections;

/// <summary>
/// Helpers for <see cref="INotifyCollectionChanged"/>
/// </summary>
internal static class CollectionChanged
{
	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Add"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs Add(object? item, int index)
		=> new(NotifyCollectionChangedAction.Add, new[] { item }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Add"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs AddSome(IList items, int index)
		=> new(NotifyCollectionChangedAction.Add, items, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Remove"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs Remove(object? item, int index)
		=> new(NotifyCollectionChangedAction.Remove, new[] { item }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Remove"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs RemoveSome(IList items, int index)
		=> new(NotifyCollectionChangedAction.Remove, items, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Replace"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs Replace(object? oldItem, object? newItem, int index)
		=> new(NotifyCollectionChangedAction.Replace, new[] { newItem }, new[] { oldItem }, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Replace"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs ReplaceSome(IList oldItems, IList newItems, int index)
		=> new(NotifyCollectionChangedAction.Replace, newItems, oldItems, index);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Move"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs Move(object? item, int oldIndex, int newIndex)
		=> new(NotifyCollectionChangedAction.Move, new[] { item }, newIndex, oldIndex);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Move"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs MoveSome(IList items, int oldIndex, int newIndex)
		=> new(NotifyCollectionChangedAction.Move, items, newIndex, oldIndex);

	/// <summary>
	/// Creates a <see cref="NotifyCollectionChangedAction.Reset"/> collection changed event args
	/// </summary>
	public static NotifyCollectionChangedEventArgs Reset() => _reset;
	private static readonly NotifyCollectionChangedEventArgs _reset = new(NotifyCollectionChangedAction.Reset);

	/// <summary>
	/// Converts the old and new items of an <see cref="NotifyCollectionChangedEventArgs"/> using a <see cref="IConverter{TFrom,TTo}"/>
	/// </summary>
	/// <typeparam name="TFrom">Type of the items in the original args</typeparam>
	/// <typeparam name="TTo">Type of the items in the result args</typeparam>
	/// <param name="args">Original args</param>
	/// <param name="converter">Converter to use to convert items</param>
	/// <returns>A <see cref="NotifyCollectionChangedEventArgs"/> of <typeparamref name="TTo"/>.</returns>
	public static NotifyCollectionChangedEventArgs Convert<TFrom, TTo>(NotifyCollectionChangedEventArgs args, IConverter<TFrom, TTo> converter)
	{
		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Add,
					new MapReadOnlyList<TFrom, TTo>(converter, args.NewItems!),
					args.NewStartingIndex);

			case NotifyCollectionChangedAction.Move:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Move,
					new MapReadOnlyList<TFrom, TTo>(converter, args.NewItems!),
					args.NewStartingIndex,
					args.OldStartingIndex);

			case NotifyCollectionChangedAction.Replace:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Replace,
					new MapReadOnlyList<TFrom, TTo>(converter, args.NewItems!),
					new MapReadOnlyList<TFrom, TTo>(converter, args.OldItems!),
					args.NewStartingIndex);

			case NotifyCollectionChangedAction.Remove:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Remove,
					new MapReadOnlyList<TFrom, TTo>(converter, args.OldItems!),
					args.OldStartingIndex);

			case NotifyCollectionChangedAction.Reset:
				return args; // No items collection to map, we can just forward the args

			default:
				throw new ArgumentOutOfRangeException("args.Action", $"Unknown collection type action {args.Action}");
		}
	}

	/// <summary>
	/// Apply an offset to the old and new items indexes of an <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <param name="args">Original args</param>
	/// <param name="offset">The offset to apply to the indexes</param>
	public static NotifyCollectionChangedEventArgs Offset(NotifyCollectionChangedEventArgs args, int offset)
	{
		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Add,
					args.NewItems,
					args.NewStartingIndex + offset);

			case NotifyCollectionChangedAction.Move:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Move,
					args.NewItems,
					args.NewStartingIndex + offset,
					args.OldStartingIndex + offset);

			case NotifyCollectionChangedAction.Replace:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Replace,
					args.NewItems!,
					args.OldItems!,
					args.NewStartingIndex + offset);

			case NotifyCollectionChangedAction.Remove:
				return new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Remove,
					args.OldItems,
					args.OldStartingIndex + offset);

			case NotifyCollectionChangedAction.Reset:
				return args; // No index to offset, we can just forward the args

			default:
				throw new ArgumentOutOfRangeException("args.Action", $"Unknown collection type action {args.Action}");
		}
	}

	/// <summary>
	/// Converts a single <see cref="NotifyCollectionChangedEventArgs"/> for multiple items items into multiple args for a single item.
	/// </summary>
	/// <param name="arg">The source args</param>
	/// <returns>Some args equivalent to source arg.</returns>
	public static IEnumerable<NotifyCollectionChangedEventArgs> ToSingleItemChanges(NotifyCollectionChangedEventArgs arg)
	{
		switch (arg.Action)
		{
			case NotifyCollectionChangedAction.Add when arg.NewItems!.Count > 1:
				for (var i = 0; i < arg.NewItems.Count; i++)
				{
					yield return Add(arg.NewItems[i], arg.NewStartingIndex + i);
				}
				break;

			case NotifyCollectionChangedAction.Move when arg.NewItems!.Count > 1:
				if (arg.NewStartingIndex > arg.OldStartingIndex)
				{
					for (var i = 0; i < arg.NewItems.Count; i++)
					{
						yield return Move(arg.NewItems[i], arg.OldStartingIndex, arg.NewStartingIndex);
					}
				}
				else
				{
					for (var i = arg.NewItems.Count - 1; i >= 0; i--)
					{
						yield return Move(arg.NewItems[i], arg.OldStartingIndex + i, arg.NewStartingIndex);
					}
				}
				break;

			case NotifyCollectionChangedAction.Replace when arg.OldItems!.Count > 1 || arg.NewItems!.Count > 1:
				for (var i = 0; i < Math.Min(arg.NewItems!.Count, arg.OldItems.Count); i++)
				{
					yield return Replace(arg.OldItems[i], arg.NewItems[i], arg.OldStartingIndex + i);
				}
				for (var i = arg.OldItems.Count; i < arg.NewItems.Count; i++)
				{
					yield return Add(arg.NewItems[i], arg.OldStartingIndex + i);
				}
				for (var i = arg.NewItems.Count; i < arg.OldItems.Count; i++)
				{
					yield return Remove(arg.NewItems[i], arg.OldStartingIndex + i);
				}
				break;

			case NotifyCollectionChangedAction.Remove when arg.OldItems!.Count > 1:
				for (var i = 0; i < arg.OldItems.Count; i++)
				{
					yield return Remove(arg.OldItems[i], arg.OldStartingIndex);
				}
				break;

			case NotifyCollectionChangedAction.Reset:
			default:
				yield return arg;
				break;
		}
	}

	internal static void RaiseItemPerItem(NotifyCollectionChangedEventArgs arg, Action<NotifyCollectionChangedEventArgs> raise, ref int countCorrection)
	{
		switch (arg.Action)
		{
			case NotifyCollectionChangedAction.Add when arg.NewItems!.Count > 1:
				for (var i = 0; i < arg.NewItems.Count; i++)
				{
					countCorrection += 1;
					raise(Add(arg.NewItems[i], arg.NewStartingIndex + i));
				}
				break;

			case NotifyCollectionChangedAction.Move when arg.NewItems!.Count > 1:
				if (arg.NewStartingIndex > arg.OldStartingIndex)
				{
					for (var i = 0; i < arg.NewItems.Count; i++)
					{
						raise(Move(arg.NewItems[i], arg.OldStartingIndex, arg.NewStartingIndex));
					}
				}
				else
				{
					for (var i = arg.NewItems.Count - 1; i >= 0; i--)
					{
						raise(Move(arg.NewItems[i], arg.OldStartingIndex + i, arg.NewStartingIndex));
					}
				}
				break;

			case NotifyCollectionChangedAction.Replace when arg.OldItems!.Count > 1 || arg.NewItems!.Count > 1:
				for (var i = 0; i < Math.Min(arg.NewItems!.Count, arg.OldItems.Count); i++)
				{
					raise(Replace(arg.OldItems[i], arg.NewItems[i], arg.OldStartingIndex + i));
				}

				// Add extra items
				for (var i = arg.OldItems.Count; i < arg.NewItems.Count; i++)
				{
					countCorrection += 1;
					raise(Add(arg.NewItems[i], arg.OldStartingIndex + i));
				}

				// Remove trailing items
				var removeIndex = arg.OldStartingIndex + arg.NewItems.Count;
				for (var i = arg.NewItems.Count; i < arg.OldItems.Count; i++)
				{
					countCorrection -= 1;
					raise(Remove(arg.OldItems[i], removeIndex));
				}
				break;

			case NotifyCollectionChangedAction.Remove when arg.OldItems!.Count > 1:
				for (var i = 0; i < arg.OldItems.Count; i++)
				{
					countCorrection -= 1;
					raise(Remove(arg.OldItems[i], arg.OldStartingIndex));
				}
				break;

			case NotifyCollectionChangedAction.Reset:
			default:
				countCorrection = 0;
				raise(arg);
				break;
		}
	}

	/// <summary>
	/// Gets the effect that the given <see cref="NotifyCollectionChangedEventArgs"/> has on the 'Count' of the source collection.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="ignoreReset">A bool which indicates if the <see cref="NotifyCollectionChangedAction.Reset"/> should be ignored (returns 0) instead of raising an exception.</param>
	/// <returns>The number of items that will be added or removed. '</returns>
	public static int GetCountCorrection(NotifyCollectionChangedEventArgs args, bool ignoreReset = false)
	{
		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				return args.NewItems!.Count;

			case NotifyCollectionChangedAction.Remove:
				return -args.OldItems!.Count;

			case NotifyCollectionChangedAction.Replace:
				return args.NewItems!.Count - args.OldItems!.Count;

			case NotifyCollectionChangedAction.Reset when !ignoreReset:
				throw new ArgumentOutOfRangeException(nameof(args.Action));

			case NotifyCollectionChangedAction.Move:
			default:
				return 0;
		}
	}
}
