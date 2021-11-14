using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Uno.Logging;

namespace Uno.Extensions.Reactive;

[ContentProperty(Name = nameof(ValueTemplate))]
public partial class FeedView : Control
{
	#region Source DP
	public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
		"Source", typeof(object), typeof(FeedView), new PropertyMetadata(default(object), OnSourceChanged));

	private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		=> (obj as FeedView)?.Subscribe(args.NewValue as ISignal<IMessage>);

	public object? Source
	{
		get => GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}
	#endregion

	#region VisualStateSelector DP
	public static readonly DependencyProperty VisualStateSelectorProperty = DependencyProperty.Register(
		"VisualStateSelector", typeof(FeedViewVisualStateSelector), typeof(FeedView), new PropertyMetadata(new FeedViewVisualStateSelector()));

	public FeedViewVisualStateSelector? VisualStateSelector
	{
		get => (FeedViewVisualStateSelector)GetValue(VisualStateSelectorProperty);
		set => SetValue(VisualStateSelectorProperty, value);
	}
	#endregion

	#region State DP (read only)
	public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
		"State", typeof(FeedViewState), typeof(FeedView), new PropertyMetadata(new FeedViewState()));

	public FeedViewState State
	{
		get => (FeedViewState)GetValue(StateProperty);
		private set => SetValue(StateProperty, value);
	}
	#endregion

	#region ValueTemplate (DP)
	public static readonly DependencyProperty ValueTemplateProperty = DependencyProperty.Register(
		"ValueTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	public DataTemplate? ValueTemplate
	{
		get => (DataTemplate)GetValue(ValueTemplateProperty);
		set => SetValue(ValueTemplateProperty, value);
	}
	#endregion

	#region Undefined (DP)
	public static readonly DependencyProperty UndefinedProperty = DependencyProperty.Register(
		"Undefined", typeof(object), typeof(FeedView), new PropertyMetadata(default(object)));

	public object? Undefined
	{
		get => GetValue(UndefinedProperty);
		set => SetValue(UndefinedProperty, value);
	}
	#endregion

	#region UndefinedTemplate (DP)
	public static readonly DependencyProperty UndefinedTemplateProperty = DependencyProperty.Register(
		"UndefinedTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	public DataTemplate? UndefinedTemplate
	{
		get => (DataTemplate)GetValue(UndefinedTemplateProperty);
		set => SetValue(UndefinedTemplateProperty, value);
	}
	#endregion

	#region None (DP)
	public static readonly DependencyProperty NoneProperty = DependencyProperty.Register(
		"None", typeof(object), typeof(FeedView), new PropertyMetadata(default(object)));

	public object? None
	{
		get => GetValue(NoneProperty);
		set => SetValue(NoneProperty, value);
	}
	#endregion

	#region NoneTemplate (DP)
	public static readonly DependencyProperty NoneTemplateProperty = DependencyProperty.Register(
		"NoneTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	public DataTemplate? NoneTemplate
	{
		get => (DataTemplate)GetValue(NoneTemplateProperty);
		set => SetValue(NoneTemplateProperty, value);
	}
	#endregion

	#region ProgressTemplate (DP)
	public static readonly DependencyProperty ProgressTemplateProperty = DependencyProperty.Register(
		"ProgressTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	public DataTemplate ProgressTemplate
	{
		get => (DataTemplate)GetValue(ProgressTemplateProperty);
		set => SetValue(ProgressTemplateProperty, value);
	}
	#endregion

	#region ErrorTemplate (DP)
	public static readonly DependencyProperty ErrorTemplateProperty = DependencyProperty.Register(
	"ErrorTemplate", typeof(DataTemplate), typeof(FeedView), new PropertyMetadata(default(DataTemplate)));

	public DataTemplate ErrorTemplate
	{
		get => (DataTemplate)GetValue(ErrorTemplateProperty);
		set => SetValue(ErrorTemplateProperty, value);
	} 
	#endregion

	private bool _isReady;
	private Subscription? _subscription;

	public FeedView()
	{
		if (Debugger.IsAttached)
		{
			ViewDebugger.SetIsEnabled(this, true);
		}

		State = new FeedViewState { Parent = DataContext }; // Create a State instance specific for this FeedView

		//RegisterPropertyChangedCallback(
		//	DataContextProperty,
		//	(obj, _) =>
		//	{
		//		if (obj is FeedView that)
		//		{
		//			that.State.Parent = that.DataContext;
		//		}
		//	});
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
				this.Log().Error("Subscription to feed failed, view will no longer render updates made by the VM.", error);
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
				this.Log().Error("Failed to change visual state.", error);
			}
		}

		/// <inheritdoc />
		public void Dispose()
			=> _ct.Cancel();
	}
}
