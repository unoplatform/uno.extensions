using Microsoft.Maui.ApplicationModel;
using Uno.Extensions.Maui.Platform;

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
			var app = MauiApplication.Current;
			var mauiContext = MauiApplication.Current.Handler.MauiContext;

			// Allow the use of Dependency Injection for the View
			var instance = ActivatorUtilities.CreateInstance(mauiContext.Services, type);
			if(instance is VisualElement visualElement)
			{
				mauiHost.Content = visualElement;
				visualElement.Parent = app;
				visualElement.BindingContext = mauiHost.DataContext;
			}
			else
			{
				throw new MauiEmbeddingException(string.Format(Properties.Resources.TypeMustInheritFromPageOrView, instance.GetType().FullName));
			}

			var native = visualElement.ToPlatform(mauiContext);
			mauiHost.NativeView = native;

			mauiHost.MauiContentCreated.Invoke(mauiHost, new MauiContentCreatedEventArgs(visualElement));
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

	private object NativeView
	{
		get => base.Content;
		set => base.Content = value;
	}

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
	}

	public event EventHandler<MauiContentCreatedEventArgs> MauiContentCreated = delegate { };

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
		var page = GetPage(Content);
		page?.SendAppearing();
	}

	private void OnMauiContentUnloaded(object sender, RoutedEventArgs e)
	{
		var page = GetPage(Content);
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

	public new VisualElement? Content { get; private set; }

	/// <summary>
	/// Gets or sets the <see cref="Type"/> of the Maui Content Source
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

		if (!_initializedResources && Content is not null)
		{
			Content.Resources.MergedDictionaries.Add(resources.ToMauiResources());
			_initializedResources = true;
		}
	}

	void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (Content is not null &&
			Content.BindingContext != DataContext)
		{
			Content.BindingContext = DataContext;
		}
	}
#endif
}
