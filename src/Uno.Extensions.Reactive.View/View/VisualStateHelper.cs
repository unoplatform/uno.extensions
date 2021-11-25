using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.Extensions.Reactive;

/// <summary>
/// An helper class that makes sure to not re-request the same visual state again and again.
/// </summary>
internal class VisualStateHelper
{
	private readonly Control _control;
	private Dictionary<string, GroupInfo>? _states;

	public VisualStateHelper(Control control)
	{
		_control = control;
	}

	public void GoToState(string stateName, bool useTransitions)
	{
		_states ??= BuildStatesCache();

		if (_states?.TryGetValue(stateName, out var group) ?? false)
		{
			if (group.LastRequestedState != stateName)
			{
				group.LastRequestedState = stateName;
				VisualStateManager.GoToState(_control, stateName, useTransitions);
			}
		}
		else
		{
			VisualStateManager.GoToState(_control, stateName, useTransitions);
		}
	}

	private Dictionary<string, GroupInfo>? BuildStatesCache()
	{
		var root = VisualTreeHelper.GetChild(_control, 0);
		if (root is FrameworkElement elt)
		{
			return VisualStateManager
				.GetVisualStateGroups(elt)
				.SelectMany(group =>
				{
					var info = new GroupInfo(group);
					return group.States.Select(state => (name: state.Name, group: info));
				})
				.Distinct(CacheEqualityComparer.Instance)
				.ToDictionary(state => state.name, state => state.group);
		}
		else
		{
			return default;
		}
	}

	private class GroupInfo
	{
		public GroupInfo(VisualStateGroup group)
		{
			Name = group.Name;
			LastRequestedState = group.CurrentState?.Name;
		}

		public string Name { get; }

		public string? LastRequestedState { get; set; }
	}

	private class CacheEqualityComparer : EqualityComparer<(string name, GroupInfo group)>
	{
		public static CacheEqualityComparer Instance { get; } = new();

		private CacheEqualityComparer()
		{
		}

		/// <inheritdoc />
		public override bool Equals((string name, GroupInfo group) x, (string name, GroupInfo group) y)
			=> x.name == y.name;

		/// <inheritdoc />
		public override int GetHashCode((string name, GroupInfo group) obj)
			=> obj.name.GetHashCode();
	}
}
