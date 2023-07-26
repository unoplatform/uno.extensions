using Microsoft.Maui.Controls;

namespace Uno.Extensions.Maui;

/// <summary>
/// ContentControl implementation that hosts a Maui view.
/// </summary>
[ContentProperty(Name = nameof(View))]
public partial class UnoMauiHost : ContentControl
{
	/// <summary>
	/// The View property represents the <see cref="View"/> that will be used as content.
	/// </summary>
	public static readonly DependencyProperty ViewProperty =
		DependencyProperty.Register(nameof(View), typeof(View), typeof(UnoMauiHost), new PropertyMetadata(null, OnViewChanged));

	private static void OnViewChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (args.NewValue is null ||
			args.NewValue is not View view ||
			dependencyObject is not UnoMauiHost mauiHost)
		{
			return;
		}

		mauiHost.Content = view;
	}

	private static ILogger GetLogger() =>
		MauiEmbedding.MauiContext.Services.GetRequiredService<ILogger<UnoMauiHost>>();

	private MauiContentHost? _host;

	private readonly IMauiContext MauiContext;

	/// <summary>
	/// Initializes a new instance of the MauiContent class.
	/// </summary>
	public UnoMauiHost()
	{
		this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		this.VerticalContentAlignment = VerticalAlignment.Stretch;

		MauiContext = MauiEmbedding.MauiContext;
		Loading += OnLoading;
		DataContextChanged += OnDataContextChanged;
		Unloaded += OnMauiContentUnloaded;
	}

	private void OnMauiContentUnloaded(object sender, RoutedEventArgs e)
	{
		Unloaded -= OnMauiContentUnloaded;
		Loading -= OnLoading;
		DataContextChanged -= OnDataContextChanged;
		if (_host is not null)
		{
			_host.BindingContext = null;
		}
		_host = null;
	}

	/// <summary>
	/// Gets or sets the <see cref="View"/> that will be used as content.
	/// </summary>
	public View View
	{
		get => (View)GetValue(ViewProperty);
		set => SetValue(ViewProperty, value);
	}

	private void OnLoading(FrameworkElement sender, object args)
	{
		Loading -= OnLoading;
		DependencyObject? treeElement = this;
		var resources = new ResourceDictionary();
		while (treeElement is not null)
		{
			if (treeElement is FrameworkElement element && element.Resources.Any())
			{
				foreach ((var key, var value) in element.Resources)
				{
					if (resources.ContainsKey(key))
					{
						continue;
					}
					resources[key] = value;
				}
			}

			treeElement = VisualTreeHelper.GetParent(treeElement);
		}

		if (_host is null)
		{
			_host = new MauiContentHost(resources)
			{
				BindingContext = DataContext,
				Content = View

			};

			try
			{
				var native = _host.ToPlatform(MauiContext);
				Content = native;
			}
			catch (Exception ex)
			{
				var logger = GetLogger();
				if (logger.IsEnabled(LogLevel.Error))
				{
					logger.LogError(ex, Properties.Resources.UnableToConvertMauiViewToNativeView);
				}
#if DEBUG
				System.Diagnostics.Debugger.Break();
#endif
				throw new MauiEmbeddingException(Properties.Resources.UnexpectedErrorConvertingMauiViewToNativeView, ex);
			}

		}
	}

	void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (_host is not null &&
			_host.BindingContext != DataContext)
		{
			_host.BindingContext = DataContext;
		}
	}
}
