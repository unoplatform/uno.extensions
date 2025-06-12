using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Uno.Extensions.Maui.Extensibility;
using Uno.Extensions.Maui.Platform;
using Windows.Foundation;

namespace Uno.Extensions.Maui;

/// <summary>
/// ContentControl implementation that hosts a Maui view.
/// </summary>
public partial class MauiHost : ContentControl
{
	private static object locker = new object();

	/// <summary>
	/// The Maui Source property represents the type of the Maui View to create
	/// </summary>
	public static readonly DependencyProperty SourceProperty =
		DependencyProperty.Register(nameof(Source), typeof(Type), typeof(MauiHost), new PropertyMetadata(null, OnSourceChanged));

	private static void OnSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		MauiEmbeddingExtension.Default.OnSourceChanged(dependencyObject, args);
	}

#if MAUI_EMBEDDING

	private static ILogger GetLogger() =>
		IPlatformApplication.Current?.Services.GetRequiredService<ILogger<MauiHost>>() ?? throw new MauiEmbeddingInitializationException();

	/// <summary>
	/// Initializes a new instance of the MauiContent class.
	/// </summary>
	public MauiHost()
	{
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;

		Loading += OnLoading;
		Loaded += OnMauiContentLoaded;
		DataContextChanged += OnDataContextChanged;
		Unloaded += OnMauiContentUnloaded;
		ActualThemeChanged += OnActualThemeChanged;
		SizeChanged += OnSizeChanged;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		MauiEmbeddingExtension.Default.OnSizeChanged(VisualElement);
	}

	private void OnActualThemeChanged(FrameworkElement sender, object args)
	{
		if (IPlatformApplication.Current is null || IPlatformApplication.Current.Application is not MauiApplication app)
			return;

		lock(locker)
		{
			// Try to prevent multiple updates if there are multiple Hosts within an App
			var theme = sender.ActualTheme switch
			{
				ElementTheme.Dark => AppTheme.Dark,
				ElementTheme.Light => AppTheme.Light,
				_ => AppTheme.Unspecified
			};

			if (app.UserAppTheme != theme)
			{
				app.UserAppTheme = theme;
			}
		}
	}

	private void OnMauiContentLoaded(object sender, RoutedEventArgs e)
	{
		var page = GetPage(VisualElement);
		page?.SendAppearing();
	}

	private void OnMauiContentUnloaded(object sender, RoutedEventArgs e)
	{
		var page = GetPage(VisualElement);
		page?.SendDisappearing();
	}

	private Microsoft.Maui.Controls.Page? GetPage(Element? element) =>
		element switch
		{
			Microsoft.Maui.Controls.Page page => page,
			null => null,
			_ => GetPage(element.Parent)
		};
#endif



	/// <summary>
	/// Fires when the VisualElement has changed.
	/// </summary>
	public event EventHandler<VisualElementChangedEventArgs> VisualElementChanged = delegate { };

	/// <summary>
	/// Gets the Maui <see cref="VisualElement"/> created by the Source.
	/// </summary>
	public VisualElement? VisualElement { get; internal set; }

	/// <summary>
	/// Gets or sets the <see cref="Type"/> of the Maui Content Source.
	/// </summary>
	public Type? Source
	{
		get => (Type?)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	internal void InvokeVisualElementChanged(VisualElement visualElement)
	{
		VisualElementChanged.Invoke(this, new VisualElementChangedEventArgs(visualElement));
	}

	internal ILogger GetLoggerInternal()
	{
		return GetLogger();
	}

#if MAUI_EMBEDDING

	private bool _initializedResources;
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

		if (!_initializedResources && VisualElement is not null)
		{
			VisualElement.Resources.MergedDictionaries.Add(resources.ToMauiResources());
			_initializedResources = true;
		}
	}

	void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (VisualElement is not null &&
			VisualElement.BindingContext != DataContext)
		{
			VisualElement.BindingContext = DataContext;
		}
	}
#endif
}
