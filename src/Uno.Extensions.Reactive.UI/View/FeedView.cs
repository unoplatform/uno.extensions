using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// A control to render <see cref="IFeed{T}"/>
/// </summary>
[ContentProperty(Name = nameof(ValueTemplate))]
public partial class FeedView : Control
{
	#region Source DP
	/// <summary>
	/// Backing dependency property for <see cref="Source"/>.
	/// </summary>
	public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
		"Source", typeof(object), typeof(FeedView), new PropertyMetadata(default(object), OnSourceChanged));

	private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		=> (obj as FeedView)?.Subscribe();

	/// <summary>
	/// Gets or sets the <see cref="IFeed{T}"/> displayed by this control.
	/// </summary>
	public object? Source
	{
		get => GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}
	#endregion

	#region VisualStateSelector DP
	/// <summary>
	/// Backing dependency property for <see cref="VisualStateSelector"/>.
	/// </summary>
	public static readonly DependencyProperty VisualStateSelectorProperty = DependencyProperty.Register(
		"VisualStateSelector", typeof(FeedViewVisualStateSelector), typeof(FeedView), new PropertyMetadata(new FeedViewVisualStateSelector()));

	/// <summary>
	/// The selector to use to select visual state.
	/// </summary>
	public FeedViewVisualStateSelector? VisualStateSelector
	{
		get => (FeedViewVisualStateSelector)GetValue(VisualStateSelectorProperty);
		set => SetValue(VisualStateSelectorProperty, value);
	}
	#endregion

	#region State DP (read only)
	/// <summary>
	/// Backing dependency property for <see cref="State"/>.
	/// </summary>
	public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
		"State", typeof(FeedViewState), typeof(FeedView), new PropertyMetadata(default)); // Default value set in the ctor

	/// <summary>
	/// The state object that expose the values to template bindings.
	/// </summary>
	public FeedViewState State
	{
		get => (FeedViewState)GetValue(StateProperty);
		private set => SetValue(StateProperty, value);
	}
	#endregion

	#region ValueTemplate (DP)
	/// <summary>
	/// Backing dependency property for <see cref="ValueTemplate"/>.
	/// </summary>
	public static readonly DependencyProperty ValueTemplateProperty = DependencyProperty.Register(
		"ValueTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	/// <summary>
	/// The template to use to render the value of a feed.
	/// </summary>
	public DataTemplate? ValueTemplate
	{
		get => (DataTemplate)GetValue(ValueTemplateProperty);
		set => SetValue(ValueTemplateProperty, value);
	}
	#endregion

	#region Undefined (DP)
	/// <summary>
	/// Backing dependency property for <see cref="Undefined"/>.
	/// </summary>
	public static readonly DependencyProperty UndefinedProperty = DependencyProperty.Register(
		"Undefined", typeof(object), typeof(FeedView), new PropertyMetadata(default(object)));

	/// <summary>
	/// The content to display when feed has <see cref="Option.Undefined{T}"/> data.
	/// </summary>
	public object? Undefined
	{
		get => GetValue(UndefinedProperty);
		set => SetValue(UndefinedProperty, value);
	}
	#endregion

	#region UndefinedTemplate (DP)
	/// <summary>
	/// Backing dependency property for <see cref="UndefinedTemplate"/>.
	/// </summary>
	public static readonly DependencyProperty UndefinedTemplateProperty = DependencyProperty.Register(
		"UndefinedTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	/// <summary>
	/// The template to use to render <see cref="Undefined"/> content.
	/// </summary>
	public DataTemplate? UndefinedTemplate
	{
		get => (DataTemplate)GetValue(UndefinedTemplateProperty);
		set => SetValue(UndefinedTemplateProperty, value);
	}
	#endregion

	#region None (DP)
	/// <summary>
	/// Backing dependency property for <see cref="None"/>.
	/// </summary>
	public static readonly DependencyProperty NoneProperty = DependencyProperty.Register(
		"None", typeof(object), typeof(FeedView), new PropertyMetadata(default(object)));

	/// <summary>
	/// The content to display when feed has <see cref="Option.None{T}"/> data.
	/// </summary>
	public object? None
	{
		get => GetValue(NoneProperty);
		set => SetValue(NoneProperty, value);
	}
	#endregion

	#region NoneTemplate (DP)
	/// <summary>
	/// Backing dependency property for <see cref="ErrorTemplate"/>.
	/// </summary>
	public static readonly DependencyProperty NoneTemplateProperty = DependencyProperty.Register(
		"NoneTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	/// <summary>
	/// The template to use to render <see cref="None"/> content.
	/// </summary>
	public DataTemplate? NoneTemplate
	{
		get => (DataTemplate)GetValue(NoneTemplateProperty);
		set => SetValue(NoneTemplateProperty, value);
	}
	#endregion

	#region ProgressTemplate (DP)
	/// <summary>
	/// Backing dependency property for <see cref="ProgressTemplate"/>.
	/// </summary>
	public static readonly DependencyProperty ProgressTemplateProperty = DependencyProperty.Register(
		"ProgressTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	/// <summary>
	/// The template to use to render feed's progress.
	/// </summary>
	public DataTemplate ProgressTemplate
	{
		get => (DataTemplate)GetValue(ProgressTemplateProperty);
		set => SetValue(ProgressTemplateProperty, value);
	}
	#endregion

	#region ErrorTemplate (DP)
	/// <summary>
	/// Backing dependency property for <see cref="ErrorTemplate"/>.
	/// </summary>
	public static readonly DependencyProperty ErrorTemplateProperty = DependencyProperty.Register(
		"ErrorTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	/// <summary>
	/// The template to use to render feed's error.
	/// </summary>
	public DataTemplate ErrorTemplate
	{
		get => (DataTemplate)GetValue(ErrorTemplateProperty);
		set => SetValue(ErrorTemplateProperty, value);
	}
	#endregion

	#region RefreshingState (DP)
	/// <summary>
	/// Backing dependency property for <see cref="RefreshingState"/>.
	/// </summary>
	public static readonly DependencyProperty RefreshingStateProperty = DependencyProperty.Register(
		"RefreshingState", typeof(FeedViewRefreshState), typeof(FeedView), new PropertyMetadata(FeedViewRefreshState.Default));

	/// <summary>
	/// Defines the visual state that should be used for refresh.
	/// </summary>
	public FeedViewRefreshState RefreshingState
	{
		get => (FeedViewRefreshState)GetValue(RefreshingStateProperty);
		set => SetValue(RefreshingStateProperty, value);
	} 
	#endregion

	private bool _isReady;
	private Subscription? _subscription;

	/// <summary>
	/// Gets a command which request to refresh the source when executed.
	/// </summary>
	public IAsyncCommand Refresh { get; }

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	public FeedView()
	{
		if (Debugger.IsAttached)
		{
			ViewDebugger.SetIsEnabled(this, true);
		}

		DefaultStyleKey = typeof(FeedView);

		Refresh = new RefreshCommand(this);
		State = new FeedViewState(this) { Parent = DataContext }; // Create a State instance specific for this FeedView

		SetBinding(ReroutedDataContextProperty, new Binding());

		Loaded += Enable;
		Unloaded += Disable;
	}

	private static readonly DependencyProperty ReroutedDataContextProperty = DependencyProperty.Register(
		"ReroutedDataContext", typeof(object), typeof(FeedView), new PropertyMetadata(default(object), (snd, e) => ((FeedView)snd).State.Parent = e.NewValue));

	private static readonly DependencyProperty SourceFeedProperty = DependencyProperty.Register(
		"SourceFeed", typeof(object), typeof(FeedView), new PropertyMetadata(default(object), OnSourceChanged));

	private WeakReference<Binding>? _sourceBinding; // Keep a ref to the binding to the SourceProperty

	private static void Enable(object snd, RoutedEventArgs _)
	{
		if (snd is FeedView that)
		{
			that._isReady = true;
			that.Subscribe();
		}
	}

	private static void Disable(object snd, RoutedEventArgs _)
	{
		if (snd is FeedView that)
		{
			that._isReady = false;
			that._subscription?.Dispose();
		}
	}

	private void Subscribe()
	{
		var feed = GetSourceFeed();
		if (feed is null || !_isReady)
		{
			SetIsLoading(!_isReady); // If we set the Source to null while we are already ready, we clear the loading flag.
			_subscription?.Dispose();

			return;
		}

		if (_subscription?.Feed == feed)
		{
			return;
		}

		_subscription?.Dispose();
		_subscription = new Subscription(this, feed);
	}

	private ISignal<IMessage>? GetSourceFeed()
	{
		var src = Source;
		if (src is ISignal<IMessage> feed)
		{
			return feed;
		}
		else if (GetBindingExpression(SourceProperty) is { ParentBinding: { Path.Path.Length: > 0 } srcBinding })
		{
			// The source is data-bound and is not a feed.
			// Lets try to data-bind to the same path suffixed with a "_Feed" in order to get access to the underlying feed if any.
			// This is a convention implemented by the generated VM to allow un-distinct usage of FeedView or direct binding to plain properties.

			if ((_sourceBinding?.TryGetTarget(out var lastSourceBindingUsedForFeedSource) ?? true)
				|| lastSourceBindingUsedForFeedSource != srcBinding)
			{
				// The binding to the SourceProperty has changed, we need to update binding to the internal SourceFeedProperty

				_sourceBinding = new WeakReference<Binding>(srcBinding);

				var sourceFeedBinding = new Binding
				{
					Path = new PropertyPath(srcBinding.Path.Path + "_Feed"),
					Source = srcBinding.Source,
					RelativeSource = srcBinding.RelativeSource,
					ElementName = srcBinding.ElementName,
					// We ignore Converter, Fallback and TargetNullValue properties as they do not make sense for such fallback binding
					Mode = BindingMode.OneTime, // We also hard code the mode as anyway this property is read-only
				};

				SetBinding(SourceFeedProperty, sourceFeedBinding);
			}

			return GetValue(SourceFeedProperty) as ISignal<IMessage>;
		}
		else
		{
			return null;
		}
	}
}
