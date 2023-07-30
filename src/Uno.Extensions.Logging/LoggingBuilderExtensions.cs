namespace Uno.Extensions;

/// <summary>
/// Extension methods to adjust the scope of collected logs. (log level)
/// </summary>
public static class LoggingBuilderExtensions
{
	/// <summary>
	/// Sets the log level for the Uno Platform core namespaces.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder CoreLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		// Default filters for Uno Platform namespaces
		builder.AddFilter("Uno.", logLevel);
		builder.AddFilter("Windows.", logLevel);
		builder.AddFilter("Microsoft.", logLevel);
		return builder;
	}

	/// <summary>
	/// Sets the log level for namespaces related to the XAML parser.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder XamlLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		// Generic Xaml events
		builder.AddFilter("Microsoft.UI.Xaml", logLevel);
		builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", logLevel);
		builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", logLevel);
		builder.AddFilter("Microsoft.UI.Xaml.UIElement", logLevel);
		builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", logLevel);
		builder.AddFilter("Windows.UI.Xaml", logLevel);
		builder.AddFilter("Windows.UI.Xaml.VisualStateGroup", logLevel);
		builder.AddFilter("Windows.UI.Xaml.StateTriggerBase", logLevel);
		builder.AddFilter("Windows.UI.Xaml.UIElement", logLevel);
		builder.AddFilter("Windows.UI.Xaml.FrameworkElement", logLevel);
		return builder;
	}

	/// <summary>
	/// Sets the log level for namespaces related to XAML controls and layout.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder XamlLayoutLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		// Layouter specific messages
		builder.AddFilter("Microsoft.UI.Xaml.Controls", logLevel);
		builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", logLevel);
		builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", logLevel);
		builder.AddFilter("Windows.UI.Xaml.Controls", logLevel);
		builder.AddFilter("Windows.UI.Xaml.Controls.Layouter", logLevel);
		builder.AddFilter("Windows.UI.Xaml.Controls.Panel", logLevel);
		return builder;
	}

	/// <summary>
	/// Sets the log level for namespaces related to the WinRT Storage APIs.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder StorageLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		builder.AddFilter("Windows.Storage", logLevel);
		return builder;
	}

	/// <summary>
	/// Sets the log level for namespaces related to XAML data binding.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder XamlBindingLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		// Binding related messages
		builder.AddFilter("Microsoft.UI.Xaml.Data", logLevel);
		builder.AddFilter("Windows.UI.Xaml.Data", logLevel);
		return builder;
	}

	/// <summary>
	/// Sets the log level for Uno namespaces related to memory references from data binding.
	/// Adjusting this log level is useful when tracking memory leaks from data binding.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder BinderMemoryReferenceLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		// Binder memory references tracking
		builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", logLevel);
		return builder;
	}

	/// <summary>
	/// Sets the log level for Uno namespaces related to the hot reload and remote control feature.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder HotReloadCoreLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		// RemoteControl and HotReload related
		builder.AddFilter("Uno.UI.RemoteControl", logLevel);
		return builder;
	}

	/// <summary>
	/// Sets the log level for Uno namespaces related to the Web Assembly runtime.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="ILoggingBuilder"/> to configure.
	/// </param>
	/// <param name="logLevel">
	/// The desired <see cref="LogLevel"/> to filter which events are collected.
	/// </param>
	/// <returns>
	/// The <see cref="ILoggingBuilder"/> to allow chaining.
	/// </returns>
	public static ILoggingBuilder WebAssemblyLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
	{
		// Debug JS interop
		builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", logLevel);
		return builder;
	}
}
