using Microsoft.Maui.ApplicationModel;
using Uno.Extensions.Maui.Platform;

namespace Uno.Extensions.Maui;

/// <summary>
/// ContentControl implementation that hosts a Maui view.
/// </summary>
public partial class MauiHost : ContentControl
{
#if MAUI_EMBEDDING
	private static object locker = new object();
	private bool CanUpdateBindingContext;
#endif
	/// <summary>
	/// The Maui Source property represents the type of the Maui View to create
	/// </summary>
	public static readonly DependencyProperty SourceProperty =
		DependencyProperty.Register(nameof(Source), typeof(Type), typeof(MauiHost), new PropertyMetadata(null, OnSourceChanged));

	private static void OnSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if MAUI_EMBEDDING
		// Sanity Check
		if (IPlatformApplication.Current?.Application is null
			&& MauiApplication.Current?.Handler.MauiContext is not null)
		{
			if (Application.Current is not EmbeddingApplication embeddingApp)
			{
				throw new MauiEmbeddingInitializationException();
			}
			embeddingApp.InitializeApplication(MauiApplication.Current.Handler.MauiContext.Services, MauiApplication.Current);
		}

		if (args.NewValue is null ||
			args.NewValue is not Type type ||
			!type.IsAssignableTo(typeof(VisualElement)) ||
			dependencyObject is not MauiHost mauiHost ||
			MauiApplication.Current?.Handler?.MauiContext is null)
		{
			return;
		}

		try
		{
			mauiHost.CanUpdateBindingContext = false;
			var app = MauiApplication.Current;
			var mauiContext = MauiApplication.Current.Handler.MauiContext;

			// Allow the use of Dependency Injection for the View
			var instance = ActivatorUtilities.CreateInstance(mauiContext.Services, type);
			if(instance is VisualElement visualElement)
			{
				mauiHost.VisualElement = visualElement;

				// Validate that there isn't some sort of logic that is populating the control's Binding Context
				if (CanSetBindingContext(visualElement))
				{
					mauiHost.CanUpdateBindingContext = true;
					mauiHost.UpdateVisualElementBindingContext();
				}

				visualElement.Parent = app;
			}
			else
			{
				throw new MauiEmbeddingException(string.Format(Properties.Resources.TypeMustInheritFromPageOrView, instance.GetType().FullName));
			}

			var native = visualElement.ToPlatform(mauiContext);
			mauiHost.Content = native;

			mauiHost.VisualElementChanged.Invoke(mauiHost, new VisualElementChangedEventArgs(visualElement));
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
#endif
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
		VisualElement?.PlatformSizeChanged();
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

	private static bool CanSetBindingContext(VisualElement element)
	{
		if (element is ContentView contentView &&
			(contentView.Content.BindingContext != null || contentView.Content.IsSet(BindableObject.BindingContextProperty)))
		{
			return false;
		}

		return element.BindingContext is null && !element.IsSet(BindableObject.BindingContextProperty);
	}

	private void UpdateVisualElementBindingContext()
	{
		if (VisualElement is null || !CanUpdateBindingContext)
		{
			return;
		}

		VisualElement.BindingContext = DataContext;
		if (VisualElement is ContentView contentView)
		{
			contentView.Content.BindingContext = DataContext;
		}
	}
#endif

	/// <summary>
	/// Fires when the VisualElement has changed.
	/// </summary>
	public event EventHandler<VisualElementChangedEventArgs> VisualElementChanged = delegate { };

	/// <summary>
	/// Gets the Maui <see cref="VisualElement"/> created by the Source.
	/// </summary>
	public VisualElement? VisualElement { get; private set; }

	/// <summary>
	/// Gets or sets the <see cref="Type"/> of the Maui Content Source.
	/// </summary>
	public Type? Source
	{
		get => (Type?)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
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
		UpdateVisualElementBindingContext();
	}
#endif
}
