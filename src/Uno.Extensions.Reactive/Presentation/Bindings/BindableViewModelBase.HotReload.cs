using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.DependencyInjection;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Bindings;

partial class BindableViewModelBase
{
	private static readonly ILogger _untypedLog = LogExtensions.Log<BindableViewModelBase>();
	private static List<WeakReference<BindableViewModelBase>>? _instances;

	private Dictionary<(string name, Type valueType), object>? _propertyFeedsCache;

	internal static void HotPatch(Type bindable, Type originalModel, Type updatedModel)
	{
		try
		{
			if (_instances is null)
			{
				if (_untypedLog.IsEnabled(LogLevel.Information))
					_untypedLog.Info("Hot reload has been disabled.");

				return;
			}

			BindableViewModelBase[] instances;
			lock (_instances)
			{
				_instances.RemoveAll(wRef => !wRef.TryGetTarget(out _));
				instances = _instances
					.Select(wRef => wRef.TryGetTarget(out var inst) ? inst : null)
					.Where(inst => inst is not null && bindable.IsInstanceOfType(inst))
					.ToArray()!;
			}

			if (_untypedLog.IsEnabled(LogLevel.Information))
				_untypedLog.Info($"Found {instances.Length} instances of {bindable.Name} to hot-patch.");

			foreach (var instance in instances)
			{
				try
				{
					if (instance.Log().IsEnabled(LogLevel.Information))
						instance.Log().Info($"Hot-patch bindable {bindable.Name}:{instance.GetHashCode():X8}.");

					instance.HotPatch(originalModel, updatedModel);
				}
				catch (Exception e)
				{
					if (instance.Log().IsEnabled(LogLevel.Error))
						instance.Log().Error(e, "Failed to hot-patch bindable instance.");
				}
			}
		}
		catch (Exception e)
		{
			if (_untypedLog.IsEnabled(LogLevel.Error))
				_untypedLog.Error(e, "Failed to hot-patch bindable type.");
		}
	}

	private void InitializeHotReload()
	{
		if (!FeedConfiguration.EffectiveHotReload.HasFlag(HotReloadSupport.State))
		{
			return;
		}

		if (_instances is null)
		{
			Interlocked.CompareExchange(ref _instances, new List<WeakReference<BindableViewModelBase>>(), null);
		}

		lock (_instances)
		{
			_instances.RemoveAll(wRef => !wRef.TryGetTarget(out _));
			_instances.Add(new WeakReference<BindableViewModelBase>(this));
		}
	}

	private void HotPatch(Type originalModel, Type updatedModelType)
	{
		using var ctx = SourceContext.PreConfigure(updatedModelType, SourceContext.GetOrCreate(this));

		var model = __Reactive_CreateModelInstance(updatedModelType);

		ctx.EnsureApplied(model); // Ensure to share the same source context instance between the original and updated model.
		ServiceProviderHelper.FindProvider(this)?.SetProvider(model); // Ensure to share the same service provider instance between the original and updated model.

		__Reactive_UpdateModel(model);
	}

	/// <summary>
	/// Invoked by the hot-reload engine to create an instance of the updated model type.
	/// Default implementation will use the arguments used to create the original model to create it
	/// (cf. <see cref="__Reactive_GetModelArguments"/> and <see cref="__Reactive_TryGetMissingModelArgument"/>).
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected virtual object __Reactive_CreateModelInstance(Type updatedModelType)
	{
		var provider = ServiceProviderHelper.FindProvider(this);

		// First we try to create the instance only by using the service provider attached to this instance.
		// This is for cases like Uno.Extensions.Navigation which creates a sub-service provider that already contains parameters.
		// This is the best case as it also allows us to get new service instances if they have also been updated.
		if (provider is not null)
		{
			try
			{
				return ActivatorUtilities.CreateInstance(provider, updatedModelType);
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(error, $"Failed to create an instance of the updated model type '{updatedModelType}' using the service provider attached to this instance.");
				}
			}
		}

		// Second we try using our cached named arguments, with fallback to the service provider (if any) for missing arguments.
		var namedArguments = __Reactive_GetModelArguments();
		try
		{
			return ActivatorHelper.CreateInstance(updatedModelType, namedArguments, __Reactive_TryGetMissingModelArgument);
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error(error, $"Failed to create an instance of the updated model type '{updatedModelType}' using named arguments.");
			}
		}

		// Finally, we just try to create the instance using the service provider but this time we als provide the cached arguments.
		if (provider is not null)
		{
			try
			{
				var arguments = namedArguments.Where(arg => arg.value is not null).Select(arg => arg.value!).ToArray();
				return ActivatorUtilities.CreateInstance(provider, updatedModelType, arguments);
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(error, $"Failed to create an instance of the updated model type '{updatedModelType}' using the service provider attached to this instance.");
				}
			}
		}

		throw new InvalidOperationException($"Failed to create an instance of the updated model type '{updatedModelType}'.");
	}

	/// <summary>
	/// Invoked by the hot-reload engine to get arguments that has been used to create the original model
	/// in order to re-use then to create an instance of the updated model type.
	/// </summary>
	/// <returns>The set of arguments used to create the original instance of the model.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected virtual (Type type, string name, object? value)[] __Reactive_GetModelArguments()
		=> Array.Empty<(Type type, string name, object? value)>();

	/// <summary>
	/// Invoked by the hot-reload engine to get an argument that was not found in the original arguments list returned by the <see cref="__Reactive_GetModelArguments"/> method.
	/// Default implementation will try to resolve the missing argument from the service provider attached to this.
	/// </summary>
	/// <param name="type">Type of the missing argument.</param>
	/// <param name="name">Name of the missing argument.</param>
	/// <returns>The missing argument value, or null if cannot be resolved.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected virtual object? __Reactive_TryGetMissingModelArgument(Type type, string name)
	{
		var provider = ServiceProviderHelper.FindProvider(this);
		if (provider is null)
		{
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info(
					$"There is no service provider attached to this instance. "
					+ $"To improve hot-reload support, consider to attach a service provider to your bindables using {typeof(ServiceProviderHelper)}.{nameof(ServiceProviderHelper.SetProvider)} when you initially create them."
					+ $"This will allow you to add new services to your model without restarting the app.");
			}

			return null;
		}

		try
		{
			return provider.GetService(type);
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn(error, $"Failed to resolve missing model argument '{name}' of type '{type.FullName}'.");
			}

			return null;
		}
	}

	/// <summary>
	/// Invoked by the hot-reload engine to update the model instance.
	/// </summary>
	/// <param name="updatedModel">The updated model instance.</param>
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected abstract void __Reactive_UpdateModel(object updatedModel);

	/// <summary>
	/// Invoked by the hot-reload engine to transfer the state from an old model instance to a new instance.
	/// </summary>
	/// <param name="previousModel">Old instance of the model that is being replaced.</param>
	/// <param name="updatedModel">New instance of the model.</param>
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected void __Reactive_TryPatchBindableProperties(object? previousModel, object? updatedModel)
	{
		var log = (previousModel ?? updatedModel)?.Log() ?? _untypedLog;
		var trace = log.IsEnabled(LogLevel.Trace);

		if (trace) log.Trace("Trying to transfer state from previous model to the updated one.");

		if (previousModel is null || updatedModel is null)
		{
			if (trace) log.Trace($"Cannot transfer state from previous model to the updated one as {(previousModel, updatedModel) switch { (null, null) => "previous and updated are", (null, _) => "previous is", _ => "updated is" }} null.");

			return;
		}

		var context = SourceContext.GetOrCreate(this); // The same as for previousModel et updatedModel
		Debug.Assert(SourceContext.GetOrCreate(previousModel) == context);
		Debug.Assert(SourceContext.GetOrCreate(updatedModel) == context);

		var previousModelType = previousModel.GetType();
		var updatedModelType = updatedModel.GetType();

		if (trace) log.Trace($"Transferring state from '{previousModelType}:{previousModel.GetHashCode():X8}' to '{updatedModelType}:{updatedModel.GetHashCode():X8}'.");

		var previousProperties = previousModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToDictionary(prop => prop.Name, StringComparer.Ordinal);
		var updatedProperties = updatedModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToDictionary(prop => prop.Name, StringComparer.Ordinal);
		var properties = previousProperties.Keys.Concat(updatedProperties.Keys).Distinct(StringComparer.Ordinal);

		_propertyFeedsCache ??= new();

		foreach (var property in properties)
		{
			try
			{
				previousProperties.TryGetValue(property, out var previousProperty);
				updatedProperties.TryGetValue(property, out var updatedProperty);

				var previousFeed = GetAsFeed(previousModel, previousProperty);
				var updatedFeed = GetAsFeed(updatedModel, updatedProperty);

				if (previousFeed is null && updatedFeed is null)
				{
					if (trace) log.Trace($"Property {property} was and is not a feed property.");
					continue;
				}

				if (updatedFeed is not null && previousFeed?.valueType != updatedFeed.Value.valueType)
				{
					// If the previous model doesn't have a feed property or an incompatible one,
					// search in history if a feed with the same type has already existed for that property.
					previousFeed = GetLastFeed(property, updatedFeed.Value.valueType);
				}

				if (previousFeed is null)
				{
					if (trace) log.Trace($"Property {property} was not a feed (and has never been) in the previous model(s), cannot transfer state.");
					continue;
				}

				if (updatedFeed is null || previousFeed.Value.valueType != updatedFeed.Value.valueType)
				{
					// If the feed has been removed, or if the type of the new feed is not compatible, we still make sure to disconnect the old state from its current source.
					// Doing so we prevent leakage, ensure to reflect the state of the model, and make sure we will be able to re-use the underlying state if the property is added back.
					object? replacementFeed;
					var reason = updatedFeed is null
						? "has been removed"
						: $"has changed type (was IFeed<{previousFeed.Value.valueType.Name}> and is now IFeed<{updatedFeed.Value.valueType.Name}>)";
					
					switch (FeedConfiguration.HotReloadRemovalBehavior)
					{
						case HotReloadRemovalBehavior.Error:
							if (trace) log.Trace($"Property {property} {reason}, make backing state to go in error state.");
							replacementFeed = CreateErrorFeed(previousFeed.Value.valueType, $"Property '{property}' {reason}.");
							break;
						
						case HotReloadRemovalBehavior.Clear:
							if (trace) log.Trace($"Property {property} {reason}, make backing state to go in undefined state.");
							replacementFeed = CreateUndefinedFeed(previousFeed.Value.valueType);
							break;
						
						default:
							if (trace) log.Trace($"Property {property} {reason}, keep backing state untouched.");
							replacementFeed = CreateSilentFeed(previousFeed.Value.valueType);
							break;
					}

					updatedFeed = (replacementFeed, previousFeed.Value.valueType);
				}

				TryPatchBindableProperty(updatedModel, property, previousFeed.Value.valueType, previousFeed.Value.instance, updatedFeed.Value.instance);
			}
			catch (Exception error)
			{
				if (log.IsEnabled(LogLevel.Warning)) log.Warn(error, $"Failed to transfer the state of '{property}'.");
			}
		}
	}

	private static object CreateErrorFeed(Type valueType, string message)
		=> Activator.CreateInstance(typeof(ErrorFeed<>).MakeGenericType(valueType), new InvalidOperationException(message))!;

	private static object CreateUndefinedFeed(Type valueType)
		=> Activator.CreateInstance(typeof(UndefinedFeed<>).MakeGenericType(valueType))!;

	private static object CreateSilentFeed(Type valueType)
		=> Activator.CreateInstance(typeof(SilentFeed<>).MakeGenericType(valueType))!;

	private class ErrorFeed<T> : IFeed<T>
	{
		private readonly Exception _error;

		public ErrorFeed(Exception error)
		{
			_error = error;
		}

		/// <inheritdoc />
		public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
		{
			yield return Message<T>.Initial.With().Error(_error);
		}
	}

	private class UndefinedFeed<T> : IFeed<T>
	{
		/// <inheritdoc />
		public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
		{
			yield return Message<T>.Initial;
		}
	}

	private class SilentFeed<T> : IFeed<T>
	{
		/// <inheritdoc />
		public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
		{
			yield break;
		}
	}

	private static (object instance, Type valueType)? GetAsFeed(object model, PropertyInfo? property)
		=> property is not null && IsFeed(property.PropertyType, out var valueType) && property.GetValue(model) is { } instance
			? (instance, valueType)
			: null;

	private (object instance, Type valueType)? GetLastFeed(string property, Type valueType)
		=> _propertyFeedsCache?.TryGetValue((property, valueType), out var instance) is true
			? (instance, valueType)
			: null;

	private static bool IsFeed(Type type, [NotNullWhen(true)] out Type? valueType)
	{
		var feedType = IsIFeed(type) ? type : type.GetInterfaces().FirstOrDefault(IsIFeed);
		if (feedType is null)
		{
			valueType = null;
			return false;
		}
		else
		{
			valueType = feedType.GetGenericArguments()[0];
			return true;
		}

		static bool IsIFeed(Type intf)
			=> intf is { GenericTypeArguments.Length: 1 } && intf.GetGenericTypeDefinition() == typeof(IFeed<>);
	}

	private static readonly MethodInfo? _tryPatchBindableProperty = typeof(BindableViewModelBase).GetMethod(nameof(TryPatchBindablePropertyGeneric), BindingFlags.Instance | BindingFlags.NonPublic);

	private void TryPatchBindableProperty(object model, string property, Type valueType, object previousFeed, object updatedFeed)
		=> _tryPatchBindableProperty!.MakeGenericMethod(valueType).Invoke(this, [model, property, previousFeed, updatedFeed]);

	private void TryPatchBindablePropertyGeneric<T>(object model, string property, IFeed<T> previous, IFeed<T> updated)
	{
		// Like in the `Property` method, we use the context to resolve the state that is used to back the `Bindable` of that feed.
		// (If it's already an IState, we let to the context the responsibility to re-use it or not.)
		var context = SourceContext.GetOrCreate(model);
		var state = context.GetOrCreateState(previous);
		if (state is not IHotSwapState<T> hotSwap)
		{
			if (model.Log().IsEnabled(LogLevel.Information))
			{
				model.Log().Info($"The state '{state.GetType()}' used for '{property}' does not support hot-swap.");
			}

			return;
		}

		hotSwap.HotSwap(updated);

		// We also make sure to register the state as the backing state for the updated feed.
		// This is useful for incremental updates, where we will search for state from the current `updated`.
		context.States.GetOrCreateState<IFeed<T>, IState<T>>(updated, (ctx, f) => state);
		(_propertyFeedsCache ??= new())[(property, typeof(T))] = updated;
	}
}
