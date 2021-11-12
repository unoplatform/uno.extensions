using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Uno.Logging;

namespace Uno.Extensions.Reactive;

[ContentProperty(Name = nameof(Rules))]
public class SmoothVisualStateManager : VisualStateManager
{
	private readonly Dictionary<VisualStateGroup, GroupManager> _managers = new();

	public static readonly DependencyProperty RulesProperty = DependencyProperty.Register(
		"Rules", typeof(SmoothVisualStateRuleCollection), typeof(SmoothVisualStateManager), new PropertyMetadata(default(SmoothVisualStateRuleCollection)));

	public SmoothVisualStateRuleCollection Rules
	{
		get => (SmoothVisualStateRuleCollection)GetValue(RulesProperty);
		set => SetValue(RulesProperty, value);
	}

	public SmoothVisualStateManager()
	{
		Rules = new();
	}

	/// <inheritdoc />
	protected override bool GoToStateCore(Control control, FrameworkElement templateRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
	{
		if (group is null || state is null) // Occurs on UWP even if flagged as not null :/
		{
			return base.GoToStateCore(control, templateRoot, stateName, group, state, useTransitions);
		}

		var manager = _managers.TryGetValue(group, out var m)
			? m
			: _managers[group] = new GroupManager(this);
			
		return manager.GoToState(control, templateRoot, stateName, @group, state, useTransitions);
	}

	private void BaseGoToStateCore(Control control, FrameworkElement templateRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
		=> base.GoToStateCore(control, templateRoot, stateName, group, state, useTransitions);

	private class GroupManager
	{
		private readonly SmoothVisualStateManager _owner;

		private string? _current;
		private DateTimeOffset _currentTimestamp;
		private TimeSpan _currentMinDuration = TimeSpan.Zero;

		private string? _next;
		private CancellationTokenSource? _nextCt;

		public GroupManager(SmoothVisualStateManager owner)
		{
			_owner = owner;
		}

		public bool GoToState(
			Control control,
			FrameworkElement templateRoot,
			string stateName,
			VisualStateGroup group,
			VisualState state,
			bool useTransitions)
		{
			if (_nextCt is not null and {IsCancellationRequested: false} && _next == stateName)
			{
				// We are about to activate the requested state, or we are already in that state and we didn't planned to change!
				return true;
			}

			_nextCt?.Cancel();
			_nextCt = null;

			if (_current == stateName)
			{
				// Luckily the current is the one which is already applied.
				// We cancelled the change to the other state and that's it!
				return true;
			}

			_next = stateName;

			TimeSpan delay = TimeSpan.Zero, minDuration = TimeSpan.Zero;
			foreach (var rule in _owner.Rules)
			{
				var result = rule.Get(@group, @group.CurrentState, state);
				if (result.Delay is { Ticks: > 0 } d && d > delay)
				{
					delay = d;
				}
				if (result.MinDuration is { Ticks: > 0 } min && min > minDuration)
				{
					minDuration = min;
				}
			}

			var now = DateTimeOffset.UtcNow;
			var currentDuration = now - _currentTimestamp;
			var currentRemainingDuration = _currentMinDuration - currentDuration;
			var targetDelay = (int)Math.Max(currentRemainingDuration.TotalMilliseconds, delay.TotalMilliseconds);

			if (targetDelay > 0)
			{
				var ct = _nextCt = new CancellationTokenSource();
				_ = _owner
					.Dispatcher
					.RunAsync(
						CoreDispatcherPriority.Normal,
						async () =>
						{
							try
							{
								await Task.Delay(targetDelay, ct.Token);
								if (ct.IsCancellationRequested)
								{
									return;
								}

								_currentTimestamp = DateTimeOffset.UtcNow;
								_current = stateName;
								_currentMinDuration = minDuration;
								_owner.BaseGoToStateCore(control, templateRoot, stateName, group, state, useTransitions);
							}
							catch (OperationCanceledException) { }
							catch (Exception error)
							{
								_owner.Log().Error("Failed to defer go-to-state", error);
							}
						})
					.AsTask(ct.Token);
			}
			else
			{
				_currentTimestamp = now;
				_current = stateName;
				_currentMinDuration = minDuration;
				_owner.BaseGoToStateCore(control, templateRoot, stateName, group, state, useTransitions);
			}

			return true;
		}
	}
}
