using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using Chinook.SectionsNavigation;
using Chinook.StackNavigation;

namespace Chinook.SectionsNavigation
{
	public static class SectionNavigatorExtensions
	{
		/// <summary>
		/// Gets an observable of the last page type from currently active section.
		/// The observable pushes a value whenever a navigation request is processed with the type of the last page ViewModel.
		/// </summary>
		/// <param name="sectionsNavigator">The sections navigator.</param>
		/// <returns>An observable of types.</returns>
		public static IObservable<Type> ObserveActiveSectionLastPageType(this ISectionsNavigator sectionsNavigator)
		{
			return sectionsNavigator
				.ObserveStateChanged()
				.Where(args => args.EventArgs.CurrentState.LastRequestState == NavigatorRequestState.Processed)
				.Select(args =>
				{
					var state = args.EventArgs.CurrentState;
					return state.ActiveSection?.State.Stack.LastOrDefault()?.ViewModel.GetType();
				})
				.StartWith(sectionsNavigator.State.ActiveSection?.State.Stack.LastOrDefault()?.ViewModel.GetType());
		}
	}
}
