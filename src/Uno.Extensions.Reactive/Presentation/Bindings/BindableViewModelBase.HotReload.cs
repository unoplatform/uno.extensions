using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	private static List<WeakReference<BindableViewModelBase>>? _instances;

	private static readonly ILogger _untypedLog = LogExtensions.Log<BindableViewModelBase>();

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
	protected static void __Reactive_TryPatchBindableProperties(object? previousModel, object? updatedModel)
	{
		var log = (previousModel ?? updatedModel)?.Log() ?? _untypedLog;
		var trace = log.IsEnabled(LogLevel.Trace);

		if (trace) log.Trace("Trying to transfer state from previous model to the updated one.");

		if (previousModel is null || updatedModel is null)
		{
			if (trace) log.Trace($"Cannot transfer state from previous model to the updated one as {(previousModel, updatedModel) switch { (null, null) => "previous and updated are", (null, _) => "previous is", _ => "updated is" }} null.");

			return;
		}

		var previousModelType = previousModel.GetType();
		var updatedModelType = updatedModel.GetType();

		if (trace) log.Trace($"Transferring state from '{previousModelType}:{previousModel.GetHashCode():X8}' to '{updatedModelType}:{updatedModel.GetHashCode():X8}'.");

		foreach (var previousProperty in previousModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			try
			{
				if (!IsFeed(previousModel, previousProperty, out var previousFeed, out var previousValueType))
				{
					if (trace) log.Trace($"Property {previousProperty.Name} was not a feed in the previous model, cannot transfer state.");

					continue;
				}

				if (updatedModelType.GetProperty(previousProperty.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is { } updatedProperty)
				{
					if (!IsFeed(updatedModel, updatedProperty, out var updatedFeed, out var updatedValueType))
					{
						if (trace) log.Trace($"Property {updatedProperty.Name} is no longer a feed in updated model, cannot transfer state.");

						continue;
					}

					if (previousValueType != updatedValueType)
					{
						if (log.IsEnabled(LogLevel.Information)) log.Info($"Cannot transfer state of property '{previousProperty.Name}', the type of feed is not the same (was: IFeed<{previousValueType.Name}> | is: IFeed<{updatedValueType.Name}>).");

						continue;
					}

					if (trace) log.Trace($"Property {updatedProperty.Name} has been updated, replace the source feed of backing state by the new instance.");

					TryPatchBindableProperty(previousModel, previousProperty.Name, previousValueType, previousFeed, updatedFeed);
				}
				else if (FeedConfiguration.HotReloadRemovalBehavior is HotReloadRemovalBehavior.Error)
				{
					if (trace) log.Trace($"Property {previousProperty.Name} has been removed, make backing state to go in error state.");

					TryPatchBindableProperty(updatedModel, previousProperty.Name, previousValueType, previousFeed, CreateErrorFeed(previousValueType, $"Property '{previousProperty.Name}' has been removed."));
				}
				else if (FeedConfiguration.HotReloadRemovalBehavior is HotReloadRemovalBehavior.Clear)
				{
					if (trace) log.Trace($"Property {previousProperty.Name} has been removed, make backing state to go in undefined state.");

					TryPatchBindableProperty(updatedModel, previousProperty.Name, previousValueType, previousFeed, CreateUndefinedFeed(previousValueType));
				}
				else if (trace)
				{
					log.Trace($"Property {previousProperty.Name} has been removed, keep backing state untouched.");
				}

			}
			catch (Exception error)
			{
				if (log.IsEnabled(LogLevel.Warning)) log.Warn(error, $"Failed to transfer the state of '{previousProperty.Name}'.");
			}
		}
	}


	private static object CreateErrorFeed(Type valueType, string message)
		=> Activator.CreateInstance(typeof(ErrorFeed<>).MakeGenericType(valueType), new InvalidOperationException(message));

	private static object CreateUndefinedFeed(Type valueType)
		=> Activator.CreateInstance(typeof(UndefinedFeed<>).MakeGenericType(valueType));

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

	private static bool IsFeed(object model, PropertyInfo property, [NotNullWhen(true)] out object? instance, [NotNullWhen(true)] out Type? valueType)
	{
		var type = property.PropertyType;
		var feed = IsIFeed(type)
			? type
			: property.PropertyType.GetInterfaces().FirstOrDefault(IsIFeed);

		if (feed is not null)
		{
			valueType = feed.GetGenericArguments()[0];
			instance = property.GetValue(model);
		}
		else 
		{
			valueType = null;
			instance = null;
		}

		return instance is not null;

		static bool IsIFeed(Type intf)
			=> intf is { GenericTypeArguments.Length: 1 } && intf.GetGenericTypeDefinition() == typeof(IFeed<>);
	}

	private static void TryPatchBindableProperty(object model, string property, Type valueType, object previousFeed, object updatedFeed)
	{
		// Like in the `Property` method, we use the context to resolve the state that is use to back the `Bindable` of that feed.
		// (If it's already an IState, we let to the context the responsibility to re-use it or not.)
		var context = SourceContext.GetOrCreate(model);
		var previousState = _getOrCreateState!.MakeGenericMethod(valueType).Invoke(context, new[] { previousFeed });

		var hotSwapType = typeof(IHotSwapState<>).MakeGenericType(valueType);
		if (!hotSwapType.IsInstanceOfType(previousState))
		{
			if (model.Log().IsEnabled(LogLevel.Information)) model.Log().Info($"The state '{previousState.GetType()}' used for '{property}' does not support hot-swap.");

			return;
		}

		hotSwapType.GetMethod(nameof(IHotSwapState<object>.HotSwap), BindingFlags.Instance | BindingFlags.Public)!.Invoke(previousState, new[] { updatedFeed });
	}

	private static readonly MethodInfo? _getOrCreateState = typeof(SourceContext).GetMethod(nameof(SourceContext.GetOrCreateState), BindingFlags.Instance | BindingFlags.Public);
}
