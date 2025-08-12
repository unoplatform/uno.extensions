using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// A control to render <see cref="IFeed{T}"/>
/// </summary>
[ContentProperty(Name = nameof(ValueTemplate))]
[Microsoft.UI.Xaml.Data.Bindable]
public partial class FeedView : Control
{
	#region Source DP
	/// <summary>
	/// Backing dependency property for <see cref="Source"/>.
	/// </summary>
	public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
		"Source", typeof(object), typeof(FeedView), new PropertyMetadata(default(object), OnSourceChanged));

	private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		=> (obj as FeedView)?.Subscribe(args.NewValue as ISignal<IMessage>);

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
	[DynamicDependency(nameof(ErrorTemplate))]
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

	private ControlState _state;
	private Subscription? _subscription;

	[Flags]
	private enum ControlState
	{
		IsLoaded = 1 << 0,
		HasTemplate = 1 << 1,

		IsReady = IsLoaded | HasTemplate,
	}

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

	private static void Enable(object snd, RoutedEventArgs _)
	{
		if (snd is FeedView that)
		{
			that._state |= ControlState.IsLoaded;
			if (that.Source is ISignal<IMessage> feed)
			{
				that.Subscribe(feed);
			}
		}
	}

	private static void Disable(object snd, RoutedEventArgs _)
	{
		if (snd is FeedView that)
		{
			that._state &= ~ControlState.IsLoaded;
			that._subscription?.Dispose();
		}
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_state |= ControlState.HasTemplate;
		if (Source is ISignal<IMessage> feed)
		{
			Subscribe(feed);
		}
	}

	private void Subscribe(ISignal<IMessage>? feed)
	{
		if (feed is null || _state is not ControlState.IsReady)
		{
			SetIsLoading(_state is not ControlState.IsReady); // If we set the Source to null while we are already ready, we clear the loading flag.
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
}
