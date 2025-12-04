using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// An helper class use to data-bind a value.
/// </summary>
/// <typeparam name="T">The type of the value</typeparam>
/// <remarks>This type is not thread safe and is expected to be manipulated only from the UI thread.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public class Bindable<
	[DynamicallyAccessedMembers(TRequirements)]
	T
> : IBindable, INotifyPropertyChanged, IFeed<T>
{
	internal const DynamicallyAccessedMemberTypes TRequirements = DynamicallyAccessedMemberTypes.PublicProperties;

	private static readonly IEqualityComparer<T> _comparer = typeof(T).IsValueType
		? EqualityComparer<T>.Default
		: ReferenceEqualityComparer<T>.Default; // We use ref-equality for object to avoid deep comparison on the UI thread

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	private T _value = default!; // This is going to be init by property.Subscribe(OnOwnerUpdated);, and anyway with bindings we cannot ensure non-null!
	private CancellationTokenSource? _asyncSetCt;
	private List<Action<T>>? _onUpdated;
	private bool _isInitialized = false;

	private readonly BindablePropertyInfo<T> _property;
	private readonly bool _hasValueProperty;
	private readonly bool _isDenormalizedBindable;

	/// <summary>
	/// Gets the name of the property backed by this bindable
	/// </summary>
	public string PropertyName => _property.Name;

	internal bool CanWrite => _property.CanWrite;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="property">Info of the property that is backed by this instance.</param>
	/// <exception cref="ArgumentException">If the property is invalid</exception>
	public Bindable(BindablePropertyInfo<T> property)
		: this(property, BindableConfig.Default)
	{
	}

	private protected Bindable(BindablePropertyInfo<T> property, BindableConfig config = BindableConfig.Default)
	{
		if (!property.IsValid)
		{
			throw new ArgumentException("Property IsInvalid, it must be created from a BindableViewModel or a Bindable", nameof(property));
		}

		_property = property;
		_hasValueProperty = config.HasFlag(BindableConfig.RaiseValuePropertyChanged);
		_isDenormalizedBindable = false;

		if (config.HasFlag(BindableConfig.AutoInit))
		{
			Initialize();
		}
	}

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="property">Info of the property that is backed by this instance.</param>
	/// <param name="hasValueProperty">
	/// Indicates if this instance has a property name Value which can be data bind directly instead of <see cref="GetValue"/> and <see cref="SetValue(T)"/>.
	/// Is so, the <see cref="PropertyChanged"/> will be raise accordingly.
	/// </param>
	/// <exception cref="ArgumentException">If the property is invalid</exception>
	protected Bindable(BindablePropertyInfo<T> property, bool hasValueProperty)
	{
		if (!property.IsValid)
		{
			throw new ArgumentException("Property IsInvalid, it must be created from a BindableViewModel or a Bindable", nameof(property));
		}

		_property = property;
		_hasValueProperty = hasValueProperty;
		// This is a hacky way to detect that the concrete type is a subclass,
		// but it's compliant with the generated code which should be the only one which creates derives type of this.
		// A more valid approach would be to do _isInherited = hasValueProperty || GetType() != typeof(Bindable<T>) but would involve reflection.
		// See usage of this flag to understand consequences (and why it's acceptable to not have that flag set is miss-used).
		_isDenormalizedBindable = true;

		Initialize();
	}

	private protected void Initialize()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;
			_property.Subscribe(OnOwnerUpdated);
		}
	}

	/// <summary>
	/// Get info for a bindable sub-property.
	/// </summary>
	/// <typeparam name="TProperty">The type of the sub-property.</typeparam>
	/// <param name="propertyName">The name of the sub-property.</param>
	/// <param name="get">The getter.</param>
	/// <param name="set">The setter.</param>
	/// <returns>Info that can be used to create a sub-bindable object.</returns>
	protected BindablePropertyInfo<TProperty> Property<TProperty>(
		string propertyName,
		Func<T, TProperty> get,
		Func<T, TProperty, T>? set)
		=> new(
			this,
			propertyName,
			(
				_property.Feed.SelectData(get.SomeOrNone()),
				updated =>
				{
					(_onUpdated ??= new()).Add(value => updated(get(value)));
					updated(get(_value)); // Make sure to propagate the default value
				}
			),
			_property.CanWrite && set is not null
				? (update, isLeafPropertyChanged, ct) => OnSubPropertyUpdated(propertyName, get, set, update, isLeafPropertyChanged, ct)
				: default);

	/// <summary>
	/// Gets the current value.
	/// </summary>
	/// <returns>The current value.</returns>
	public T GetValue()
		=> _value;

	/// <summary>
	/// Sets the current value.
	/// </summary>
	/// <param name="value">The current value.</param>
	/// <remarks>This is not thread safe and is expected to be invoked from the UI thread.</remarks>
	public void SetValue(T value)
		=> SetValue(value, null);

	/// <summary>
	/// Sets the current value.
	/// </summary>
	/// <param name="value">The current value.</param>
	/// <param name="changes">An optional change set describing the changes applied on the <paramref name="value" /> compared to the current value.</param>
	/// <remarks>This is not thread safe and is expected to be invoked from the UI thread.</remarks>
	internal void SetValue(T value, IChangeSet? changes)
	{
		if (!_property.CanWrite)
		{
			return;
		}

		if (SetValueCore(value, changes))
		{
			UpdateOwner(value);
		}
	}

	/// <remarks>This is not thread safe and is expected to be invoked from the UI thread.</remarks>
	private bool SetValueCore(T value, IChangeSet? changes)
	{
		if (_comparer.Equals(_value, value))
		{
			return false;
		}

		var previous = _value;
		_value = value;

		// 1. Notify sub properties that the local value has changed
		UpdateSubProperties(previous, value, changes);

		// 2. Notify UI that the local value has changed
		if (_hasValueProperty)
		{
			// The Value property is generated only if the de-normalized entity does not already have a Value property.
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
		}
		// Support for `{x:Bind GetValue()}`
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GetValue)));

		return true;
	}

	/// <remarks>This is not thread safe and is expected to be invoked from the UI thread.</remarks>
	private void OnOwnerUpdated(T value)
	{
		SetValueCore(value, changes: null);

		// Usually it's the responsibility of the parent object to raise a property change with the right name,
		// however when we use generated `BindableMyEntity : Bindable<MyEntity>`,
		// we keep the same instance of `BindableMyEntity` so bindings are not re-evaluated.
		// In that case we will also raise a 'PropertyChanged("")' in order to force the binding engine to re-evaluate all properties.
		if (_isDenormalizedBindable)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
		}
	}

	private async void UpdateOwner(T value)
	{
		try
		{
			_asyncSetCt?.Cancel();
			_asyncSetCt = new();

			await _property.Update(_ => value, true, _asyncSetCt.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception error)
		{
			this.Log().Error(
				error,
				$"Synchronization from View to ViewModel of '{_property}' failed. "
				+ "(This is a temporary error, it will be retried on next change from the View.)");
		}
	}

	/// <remarks>This is not thread safe and is expected to be invoked from the UI thread.</remarks>
	private async ValueTask OnSubPropertyUpdated<TProperty>(
		string propertyName,
		Func<T, TProperty> get,
		Func<T, TProperty, T> set,
		Func<TProperty, TProperty> update,
		bool isLeafPropertyChanged,
		CancellationToken ct)
	{
		// First updates the local value (and possibly other sub-properties that are dependent upon computed values)
		if (SetValueCore(set(_value, update(get(_value))), changes: null))
		{
			// If we effectively updated the local value, then ...

			// 1. If the sub property is a leaf property (or a complex value has been set using the SetValue on the property),
			//	  we need to notify that the property itself has changed.
			//	  (If we raise the event in other cases, we will actually "invalidate" the whole object tree)
			if (isLeafPropertyChanged)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}

			// 2. Propagate the update to the parent (i.e. update the backing state)
			// Note: We re-invoke the 'update' delegate here (instead of using the '_value') to make sure to respect ACID
			await _property.Update(t => set(t, update(get(t))), false, ct).ConfigureAwait(false);
		}
	}

	/// <remarks>This is not thread safe and is expected to be invoked from the UI thread.</remarks>
	private protected virtual void UpdateSubProperties(T previous, T current, IChangeSet? changes)
	{
		if (_onUpdated is not null)
		{
			foreach (var callback in _onUpdated)
			{
				callback(current);
			}
		}
	}

	private protected void RaisePropertyChanged(string propertyName)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
		=> _property.Feed.GetSource(context, ct);
}
