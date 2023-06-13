using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace Uno.Extensions.Maui;

public abstract class MauiExtensionBase : MarkupExtension
{
	private ILogger? _logger;
	protected ILogger Logger => _logger ??= GetLogger();

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

	private ILogger GetLogger()
	{
		var factory = MauiEmbedding.MauiContext.Services.GetRequiredService<ILoggerFactory>();
		var implemenatingType = GetType();
		return factory.CreateLogger(implemenatingType.Name);
	}

	protected abstract void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName);

	protected object? Default(Type type) =>
		type.IsValueType ? Activator.CreateInstance(type) : null;
}
