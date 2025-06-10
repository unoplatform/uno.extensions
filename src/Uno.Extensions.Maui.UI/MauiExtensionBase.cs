namespace Uno.Extensions.Maui;

/// <summary>
/// Abstract class for extending <see cref="MarkupExtension"/> in the context of <see cref="Microsoft.Maui.Controls"/>.
/// </summary>
public abstract class MauiExtensionBase : MarkupExtension
{
#if MAUI_EMBEDDING

	private ILogger? _logger;

	/// <summary>
	/// Logger to log messages during runtime.
	/// </summary>
	protected ILogger Logger => _logger ??= GetLogger();


	/// <inheritdoc/>
	protected sealed override object? ProvideValue(IXamlServiceProvider serviceProvider)
	{
		var provideValueTarget = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

		if (provideValueTarget?.TargetObject is View view && provideValueTarget.TargetProperty is ProvideValueTargetProperty targetProperty)
		{
			var declaringType = targetProperty.DeclaringType;
			var targetType = declaringType.GetRuntimeProperty(targetProperty.Name)?.PropertyType;

			void OnParented(object? sender, EventArgs args)
			{
				view.ParentChanged -= OnParented;
				var bindablePropertyInfo = targetProperty.DeclaringType.GetRuntimeField($"{targetProperty.Name}Property");
				var bindableProperty = bindablePropertyInfo?.GetValue(null) as Microsoft.Maui.Controls.BindableProperty;

				var name = targetProperty.Name;

				if (targetType is null || bindableProperty is null)
				{
					var canLog = Logger.IsEnabled(LogLevel.Debug);

					// TODO: Update with XAML Line info
					if (targetType is null && canLog)
					{
						Logger.LogDebug("The Target Type is null");
					}

					if (bindableProperty is null && canLog)
					{
						Logger.LogDebug("The BindableProperty is null");
					}
#if DEBUG
					System.Diagnostics.Debugger.Break();
#endif
					return;
				}

				SetValue(view, declaringType, targetType, bindableProperty, name);
			}
			view.ParentChanged += OnParented;

			if (targetType is not null)
			{
				return Default(targetType);
			}
		}

		return base.ProvideValue(serviceProvider);
	}

	internal ILogger GetLogger()
	{
		var factory = MauiEmbedding.MauiContext.Services.GetRequiredService<ILoggerFactory>();
		var implementingType = GetType();
		return factory.CreateLogger(implementingType.Name);
	}

	/// <summary>
	/// Abstract method to set the value of a <see cref="BindableProperty"/>.
	/// </summary>
	/// <param name="view">The view to set the property value on.</param>
	/// <param name="viewType">The type of view to set the property value on.</param>
	/// <param name="propertyType">The type of the property to set.</param>
	/// <param name="property">The <see cref="BindableProperty"/> to set.</param>
	/// <param name="propertyName">The name of the property to set.</param>
	protected abstract void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName);
#endif

	/// <summary>
	/// Returns a default value of <paramref name="type"/>.
	/// </summary>
	/// <param name="type">Type of the target.</param>
	/// <returns>Default value of <paramref name="type"/>.</returns>
	protected object? Default(Type type) =>
		type.IsValueType ? Activator.CreateInstance(type) : null;
}
