using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// A validation result forwarded from a bindable to one of its sub-bindables.
/// </summary>
/// <param name="Result">The validation result.</param>
/// <param name="Member">The member targeted by the result, relative to the sub-bindable which receives it (possibly dotted), or empty for an entity-level result.</param>
internal readonly record struct BindableValidationResult(ValidationResult Result, string Member);

/// <summary>
/// The validation errors store of a bindable object, used to implement <see cref="System.ComponentModel.INotifyDataErrorInfo"/>.
/// </summary>
/// <remarks>
/// This routes validation results to the right bindable object (so {Binding Person.FirstName} can query errors on the object which owns the FirstName property),
/// given the <see cref="ValidationResult.MemberNames"/>: a result for "Address.Street" is forwarded to the sub-bindable of "Address" as a result for "Street".
/// This type is not thread safe and is expected to be manipulated only from the UI thread.
/// </remarks>
internal sealed class BindableValidationErrors
{
	private static readonly IImmutableList<ValidationResult> _noErrors = ImmutableList<ValidationResult>.Empty;
	private static readonly IImmutableList<BindableValidationResult> _noResults = ImmutableList<BindableValidationResult>.Empty;

	private readonly Action<string> _onErrorsChanged;
	private readonly Action _onHasErrorsChanged;

	private Dictionary<string, IImmutableList<ValidationResult>>? _errors;
	private Dictionary<string, SubBindable>? _subBindables;
	private Dictionary<string, IReadOnlyList<BindableValidationResult>>? _inputs; // The last results received for each scope (cf. Update), so they can be re-routed when a sub-bindable subscribes.

	private const string AllScope = ""; // Key in _inputs of results received for the whole owner (i.e. scope = null)

	/// <summary>
	/// Creates a new store.
	/// </summary>
	/// <param name="onErrorsChanged">Callback invoked when errors of a given member (or empty for the entity-level errors) changed.</param>
	/// <param name="onHasErrorsChanged">Callback invoked when the <see cref="HasErrors"/> changed.</param>
	public BindableValidationErrors(Action<string> onErrorsChanged, Action onHasErrorsChanged)
	{
		_onErrorsChanged = onErrorsChanged;
		_onHasErrorsChanged = onHasErrorsChanged;
	}

	/// <summary>
	/// Indicates if the owner, or any of its sub-bindables, has some errors.
	/// </summary>
	public bool HasErrors
	{
		get
		{
			if (_errors is { Count: > 0 })
			{
				return true;
			}

			if (_subBindables is not null)
			{
				foreach (var subBindable in _subBindables.Values)
				{
					if (subBindable.Current.Count > 0)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Gets the errors of the given member, or the entity-level errors if null or empty.
	/// </summary>
	public IEnumerable GetErrors(string? memberName)
		=> _errors is not null && _errors.TryGetValue(memberName ?? string.Empty, out var errors)
			? errors
			: _noErrors;

	/// <summary>
	/// Registers the sub-bindable of the given member, which will receive the validation results targeting its members.
	/// </summary>
	public void Subscribe(string memberName, Action<IImmutableList<BindableValidationResult>> onUpdated)
	{
		_subBindables ??= new(StringComparer.Ordinal);
		if (!_subBindables.TryGetValue(memberName, out var subBindable))
		{
			_subBindables[memberName] = subBindable = new();
		}

		subBindable.Handlers.Add(onUpdated);
		if (subBindable.Current.Count > 0)
		{
			onUpdated(subBindable.Current);
		}
		else if (_inputs is not null)
		{
			// Results have been received before the sub-bindable subscribed (e.g. sub-bindables are created after the owner has subscribed to its own owner),
			// we re-route them so the results targeting the new sub-bindable are forwarded to it.
			if (_inputs.TryGetValue(AllScope, out var all))
			{
				Update(all, scope: null);
			}
			if (memberName.Length > 0 && _inputs.TryGetValue(memberName, out var property))
			{
				Update(property, scope: memberName);
			}
		}
	}

	/// <summary>
	/// Updates the validation results for a single property of the owner.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="results">The validation results of the property value, where member names are relative to the value (or equals to the <paramref name="propertyName"/> itself).</param>
	/// <remarks>This replaces only the errors of the given property.</remarks>
	public void UpdateProperty(string propertyName, IImmutableList<ValidationResult> results)
	{
		var scoped = new List<BindableValidationResult>(results.Count);
		foreach (var result in results)
		{
			var hasMember = false;
			foreach (var member in result.MemberNames)
			{
				hasMember = true;
				scoped.Add(new(result, member is null || member == propertyName ? string.Empty : member));
			}

			if (!hasMember)
			{
				scoped.Add(new(result, string.Empty));
			}
		}

		Update(scoped, propertyName);
	}

	/// <summary>
	/// Updates all the validation results of the owner.
	/// </summary>
	/// <param name="results">The validation results, where members are relative to the owner.</param>
	public void Update(IImmutableList<BindableValidationResult> results)
		=> Update(results, scope: null);

	private void Update(IReadOnlyList<BindableValidationResult> results, string? scope)
	{
		(_inputs ??= new(StringComparer.Ordinal))[scope ?? AllScope] = results;

		Dictionary<string, List<ValidationResult>>? updatedErrors = null;
		Dictionary<string, List<BindableValidationResult>>? updatedSubResults = null;

		foreach (var (result, member) in results)
		{
			string key, subMember;
			if (scope is not null)
			{
				// All results are about the given property, the member is relative to the property value.
				key = scope;
				subMember = member;
			}
			else if (member.IndexOf('.') is var index and >= 0)
			{
				key = member.Substring(0, index);
				subMember = member.Substring(index + 1);
			}
			else
			{
				key = member;
				subMember = string.Empty;
			}

			if (key.Length > 0 && _subBindables?.ContainsKey(key) is true)
			{
				if (subMember.Length is 0)
				{
					// The result targets the member itself: it's an error of the member for the owner, and an entity-level error for the sub-bindable.
					Add(ref updatedErrors, key, result);
				}
				Add(ref updatedSubResults, key, new BindableValidationResult(result, subMember));
			}
			else
			{
				// No sub-bindable for this member (e.g. a leaf value), so the owner is the one queried by bindings.
				Add(ref updatedErrors, key, result);
			}
		}

		var hadErrors = HasErrors;
		List<string>? changedMembers = null;

		// Update the local errors
		var keys = scope is not null
			? new[] { scope }
			: (_errors?.Keys ?? Enumerable.Empty<string>()).Concat(updatedErrors?.Keys ?? Enumerable.Empty<string>()).Distinct().ToArray();
		foreach (var key in keys)
		{
			var previous = _errors is not null && _errors.TryGetValue(key, out var p) ? p : _noErrors;
			var updated = updatedErrors is not null && updatedErrors.TryGetValue(key, out var u) ? u.ToImmutableList() : _noErrors;

			if (ValidationAxis.AreEquals(previous, updated))
			{
				continue;
			}

			if (updated.Count is 0)
			{
				_errors!.Remove(key);
			}
			else
			{
				(_errors ??= new(StringComparer.Ordinal))[key] = updated;
			}
			(changedMembers ??= new()).Add(key);
		}

		// Forward to sub-bindables
		if (_subBindables is not null)
		{
			foreach (var subBindable in _subBindables)
			{
				if (scope is not null && subBindable.Key != scope)
				{
					continue;
				}

				var updated = updatedSubResults is not null && updatedSubResults.TryGetValue(subBindable.Key, out var u) ? u.ToImmutableList() : _noResults;
				subBindable.Value.Update(updated);
			}
		}

		// Finally raise events
		if (changedMembers is not null)
		{
			foreach (var member in changedMembers)
			{
				_onErrorsChanged(member);
			}
		}
		if (hadErrors != HasErrors)
		{
			_onHasErrorsChanged();
		}
	}

	private static void Add<T>(ref Dictionary<string, List<T>>? dictionary, string key, T item)
	{
		dictionary ??= new(StringComparer.Ordinal);
		if (!dictionary.TryGetValue(key, out var list))
		{
			dictionary[key] = list = new();
		}

		if (!list.Contains(item)) // Results might target the same member multiple times (e.g. MemberNames = ["Name", "Name"])
		{
			list.Add(item);
		}
	}

	private sealed class SubBindable
	{
		public List<Action<IImmutableList<BindableValidationResult>>> Handlers { get; } = new(1);

		public IImmutableList<BindableValidationResult> Current { get; private set; } = _noResults;

		public void Update(IImmutableList<BindableValidationResult> results)
		{
			if (AreEquals(Current, results))
			{
				return;
			}

			Current = results;
			foreach (var handler in Handlers)
			{
				handler(results);
			}
		}

		private static bool AreEquals(IImmutableList<BindableValidationResult> left, IImmutableList<BindableValidationResult> right)
		{
			if (left.Count != right.Count)
			{
				return false;
			}

			for (var i = 0; i < left.Count; i++)
			{
				if (left[i].Member != right[i].Member || !ValidationAxis.AreEquals(left[i].Result, right[i].Result))
				{
					return false;
				}
			}

			return true;
		}
	}
}
