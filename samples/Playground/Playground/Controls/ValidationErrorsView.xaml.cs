using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Playground.Controls;

/// <summary>
/// Displays the state of an <see cref="INotifyDataErrorInfo"/>: it listens to <see cref="INotifyDataErrorInfo.ErrorsChanged"/>
/// and lists the result of <see cref="INotifyDataErrorInfo.GetErrors"/> for the <see cref="PropertyName"/>.
/// </summary>
public sealed partial class ValidationErrorsView : UserControl
{
	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		nameof(Source),
		typeof(object),
		typeof(ValidationErrorsView),
		new PropertyMetadata(default(object), (snd, _) => ((ValidationErrorsView)snd).OnSourceChanged()));

	public static DependencyProperty PropertyNameProperty { get; } = DependencyProperty.Register(
		nameof(PropertyName),
		typeof(string),
		typeof(ValidationErrorsView),
		new PropertyMetadata(default(string), (snd, _) => ((ValidationErrorsView)snd).Refresh()));

	public static DependencyProperty HeaderProperty { get; } = DependencyProperty.Register(
		nameof(Header),
		typeof(string),
		typeof(ValidationErrorsView),
		new PropertyMetadata(default(string), (snd, args) => ((ValidationErrorsView)snd).HeaderText.Text = args.NewValue as string ?? string.Empty));

	private INotifyDataErrorInfo? _subscribed;
	private int _errorsChangedCount;
	private string? _lastChangedProperty;

	public ValidationErrorsView()
	{
		this.InitializeComponent();

		Loaded += (_, _) => Subscribe();
		Unloaded += (_, _) => Unsubscribe(); // Avoid leaking this control through the (longer-lived) view model.
	}

	/// <summary>
	/// The object to inspect, expected to implement <see cref="INotifyDataErrorInfo"/>.
	/// </summary>
	public object? Source
	{
		get => GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// The name of the property for which errors are listed, or null for the entity-level errors.
	/// </summary>
	public string? PropertyName
	{
		get => (string?)GetValue(PropertyNameProperty);
		set => SetValue(PropertyNameProperty, value);
	}

	/// <summary>
	/// A title displayed above the errors.
	/// </summary>
	public string? Header
	{
		get => (string?)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	private void OnSourceChanged()
	{
		Unsubscribe();
		_errorsChangedCount = 0;
		_lastChangedProperty = null;

		if (IsLoaded)
		{
			Subscribe();
		}
		else
		{
			Refresh();
		}
	}

	private void Subscribe()
	{
		if (_subscribed is null && Source is INotifyDataErrorInfo source)
		{
			_subscribed = source;
			source.ErrorsChanged += OnErrorsChanged;
		}

		Refresh();
	}

	private void Unsubscribe()
	{
		if (_subscribed is { } source)
		{
			_subscribed = null;
			source.ErrorsChanged -= OnErrorsChanged;
		}
	}

	private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs args)
	{
		if (DispatcherQueue.HasThreadAccess)
		{
			OnErrorsChangedCore(args);
		}
		else
		{
			DispatcherQueue.TryEnqueue(() => OnErrorsChangedCore(args));
		}
	}

	private void OnErrorsChangedCore(DataErrorsChangedEventArgs args)
	{
		_errorsChangedCount++;
		_lastChangedProperty = args.PropertyName;
		Refresh();
	}

	private void Refresh()
	{
		if (Source is not INotifyDataErrorInfo source)
		{
			ErrorsList.ItemsSource = null;
			StatusText.Text = Source is null
				? "No source."
				: $"'{Source.GetType().Name}' does not implement INotifyDataErrorInfo.";
			return;
		}

		var errors = Format(source.GetErrors(PropertyName));
		var target = string.IsNullOrEmpty(PropertyName) ? "entity-level (null)" : $"'{PropertyName}'";
		var lastChanged = _errorsChangedCount is 0
			? "never"
			: $"{_errorsChangedCount} time(s), last for {(string.IsNullOrEmpty(_lastChangedProperty) ? "entity-level" : $"'{_lastChangedProperty}'")}";

		ErrorsList.ItemsSource = errors;
		StatusText.Text = $"{source.GetType().Name} | HasErrors: {source.HasErrors} | {errors.Count} error(s) for {target} | ErrorsChanged raised {lastChanged}";
	}

	private static List<string> Format(IEnumerable? errors)
		=> errors?
			.Cast<object?>()
			.Select(error => error switch
			{
				ValidationResult { MemberNames: var members } result when members.Any() => $"{result.ErrorMessage} [{string.Join(", ", members)}]",
				ValidationResult result => result.ErrorMessage ?? string.Empty,
				_ => error?.ToString() ?? string.Empty,
			})
			.ToList()
			?? new List<string>();
}
