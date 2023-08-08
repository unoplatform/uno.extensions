namespace Uno.Extensions.Maui;

/// <summary>
/// ContentControl implementation that hosts a Maui view.
/// </summary>
[ContentProperty(Name = nameof(MauiContent))]
public partial class MauiHost : ContentControl
{
	/// <summary>
	/// The MauiContent property represents the <see cref="MauiContent"/> that will be used as content.
	/// </summary>
	public static readonly DependencyProperty MauiContentProperty =
		DependencyProperty.Register(nameof(MauiContent), typeof(MauiView), typeof(MauiHost), new PropertyMetadata(null, OnMauiContentChanged));

	private static void OnMauiContentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if MAUI_EMBEDDING
		if (args.NewValue is null ||
			args.NewValue is not MauiView view ||
			dependencyObject is not MauiHost mauiHost)
		{
			return;
		}

		mauiHost.Content = view;
#endif
	}

#if MAUI_EMBEDDING

	private static ILogger GetLogger() =>
		MauiEmbedding.MauiContext.Services.GetRequiredService<ILogger<MauiHost>>();

	private MauiContentHost? _host;

	private readonly IMauiContext MauiContext;

	/// <summary>
	/// Initializes a new instance of the MauiContent class.
	/// </summary>
	public MauiHost()
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
#endif

	/// <summary>
	/// Gets or sets the <see cref="MauiContent"/> that will be used as content.
	/// </summary>
	public MauiView MauiContent
	{
		get => (MauiView)GetValue(MauiContentProperty);
		set => SetValue(MauiContentProperty, value);
	}

#if MAUI_EMBEDDING

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
			var resourceManager = MauiContext.Services.GetRequiredService<MauiResourceManager>();
			_host = new MauiContentHost(resourceManager.CreateMauiResources(resources))
			{
				BindingContext = DataContext,
				Content = MauiContent
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
#endif
}
