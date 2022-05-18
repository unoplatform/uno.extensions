using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

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
		"State", typeof(FeedViewState), typeof(FeedView), new PropertyMetadata(new FeedViewState()));

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

	private bool _isReady;
	private Subscription? _subscription;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	public FeedView()
	{
		if (Debugger.IsAttached)
		{
			ViewDebugger.SetIsEnabled(this, true);
		}

		State = new FeedViewState { Parent = DataContext }; // Create a State instance specific for this FeedView

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
			that._isReady = true;
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
			that._isReady = false;
			that._subscription?.Dispose();
		}
	}

	private void Subscribe(ISignal<IMessage>? feed)
	{
		if (feed is null || !_isReady)
		{
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

	private class Subscription : IDisposable
	{
		private readonly CancellationTokenSource _ct = new();
		private readonly FeedView _view;
		private readonly VisualStateHelper _visualStateManager;

		public ISignal<IMessage> Feed { get; }

		public Subscription(FeedView view, ISignal<IMessage> feed)
		{
			_view = view;
			Feed = feed;

			_visualStateManager = new VisualStateHelper(_view);
			_ = Enumerate();
		}

		private async Task Enumerate()
		{
			try
			{
				// Note: Here we expect the Feed to be an IState, so we use the Feed.GetSource instead of ctx.GetOrCreateSource().
				//		 The 'ctx' is provided only for safety to improve caching, but it's almost equivalent to SourceContext.None
				//		 (especially when using SourceContext.GetOrCreate(_view)).

				var ctx = SourceContext.Find(_view.DataContext) ?? SourceContext.GetOrCreate(_view);
				await foreach (var message in Feed.GetSource(ctx, _ct.Token).WithCancellation(_ct.Token).ConfigureAwait(true))
				{
					Update(message);
				}
			}
			catch (Exception error)
			{
				this.Log().Error(error, "Subscription to feed failed, view will no longer render updates made by the VM.");
			}
		}

		private void Update(IMessage message)
		{
			try
			{
				_view.State.Update(message);

				if (_view.VisualStateSelector?.GetVisualStates(message).ToList() is { Count: > 0 } visualStates)
				{
					foreach (var state in visualStates)
					{
						_visualStateManager.GoToState(state.stateName, state.shouldUseTransition);
					}
				}
			}
			catch (Exception error)
			{
				this.Log().Error(error, "Failed to change visual state.");
			}
		}

		/// <inheritdoc />
		public void Dispose()
			=> _ct.Cancel();
	}
}
