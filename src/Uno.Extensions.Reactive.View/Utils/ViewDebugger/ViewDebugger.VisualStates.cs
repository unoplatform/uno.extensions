using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.Extensions.Reactive;

public partial class ViewDebugger
{
	public static readonly DependencyProperty VisualStatesProperty = DependencyProperty.RegisterAttached(
		"VisualStates", typeof(string), typeof(ViewDebugger), new PropertyMetadata(default(string), OnVisualStatesChanged));

	private static void OnVisualStatesChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
		=> GetDebugger(snd)?._visualStates?.OnUpdated(args.NewValue as string);

	public static string? GetVisualStates(Control control)
		=> (string)control.GetValue(VisualStatesProperty);

	public static void SetVisualStates(Control control, string? states)
		=> control.SetValue(VisualStatesProperty, states);

	public static readonly DependencyProperty AvailableVisualStatesProperty = DependencyProperty.RegisterAttached(
		"AvailableVisualStates", typeof(string), typeof(ViewDebugger), new PropertyMetadata(default(string[]?)));

	public static string? GetAvailableVisualStates(Control control)
		=> (string?)control.GetValue(AvailableVisualStatesProperty);

	public static void SetAvailableVisualStates(Control control, string? states)
		=> control.SetValue(AvailableVisualStatesProperty, states);

	private class VisualStateTracker
	{
		private readonly Control _control;

		private bool _isEnabled;

		private IList<VisualStateGroup>? _currentGroups;
		private SortedDictionary<string, string>? _currentStates;

		public VisualStateTracker(Control control)
		{
			_control = control;
		}

		private void Enable(object sender, RoutedEventArgs e) => Enable();

		public void Enable(bool update = true)
		{
			if (_isEnabled)
			{
				return;
			}

			_control.Loaded -= Enable;
			if (!_control.IsLoaded)
			{
				_control.Loaded += Enable;
				return;
			}

			_isEnabled = true;

			var root = VisualTreeHelper.GetChild(_control, 0);
			if (root is not FrameworkElement elt)
			{
				return;
			}

			_currentGroups = VisualStateManager.GetVisualStateGroups(elt);
			_currentStates = new SortedDictionary<string, string>();

			foreach (var group in _currentGroups)
			{
				_currentStates[group.Name] = (group.CurrentState ?? group.States.FirstOrDefault())?.Name ?? "";
				group.CurrentStateChanged += OnCurrentStateChanged;
			}

			SetAvailableVisualStates(_control, string.Join("\r\n", _currentGroups.Select(group => $"{group.Name}[{string.Join("|", group.States.Select(state => state.Name))}]")));

			if (update)
			{
				Update();
			}
		}

		public void Disable()
		{
			foreach (var group in _currentGroups ?? Enumerable.Empty<VisualStateGroup>())
			{
				group.CurrentStateChanged -= OnCurrentStateChanged;
			}

			_currentGroups = null;
			_currentStates = null;
			_isEnabled = false;

			SetVisualStates(_control, null);
		}

		private void OnCurrentStateChanged(object sender, VisualStateChangedEventArgs args)
		{
			// Note: On UWP the 'sender' is NOT the VisualStateGroup O_O

			var group = _currentGroups?.FirstOrDefault(g => g.States.Contains(args.NewState));
			if (group is null)
			{
				return;
			}

			_currentStates![group.Name] = args.NewState?.Name ?? "";
			Update();
		}

		private void Update()
			=> SetVisualStates(_control, string.Join("; ", _currentStates!.Values.Where(state => !string.IsNullOrWhiteSpace(state))));

		public void OnUpdated(string? rawStates)
		{
			if (rawStates is null)
			{
				return;
			}

			Enable(update: false); // Note: setting an empty string on VisualStates DP is a way to enable the tracking!

			var states = rawStates
				.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(state => state.Trim());

			if (_currentStates is not null)
			{
				// If possible we don't request a GoToState for a state which is already active.
				// It usually prevents double GoToState for same group and also makes sure to break any possible cyclic states updates!
				states = states.Where(state => !_currentStates.ContainsValue(state));
			}

			foreach (var state in states)
			{
				VisualStateManager.GoToState(_control, state, true);
			}
		}
	}
}
