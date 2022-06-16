namespace TestHarness;

public static class PageExtensions
{
	public static void ApplyAdaptiveTrigger(this Page page, double threshold, string narrowState, string wideState)
	{
		new ResponsiveState(page, threshold, narrowState, wideState).Connect();
	}

	private record ResponsiveState(Page Page, double Threshold, string NarrowState, string WideState)
	{
		private string? _currentState;

		public void Connect()
		{
			Page.Loaded += async (s, e) => await Resize(true);
			Page.SizeChanged += async (s, e) => await Resize(false);
		}

		private async Task Resize(bool refresh)
		{
			double newWidth = Page.ActualWidth;

			if (newWidth <= 0)
			{
				return;
			}


			var newState = newWidth > Threshold ? WideState : NarrowState;
			if (_currentState != newState || refresh)
			{
				_currentState = newState;
				// Task.Yield is required as we're setting visual states in code. If the
				// Region.Attached property is set to fast, it doesn't connect up the region
				// correctly. This isn't an issue when the visual states are driven using
				// adaptive triggers
				await Task.Yield();
				VisualStateManager.GoToState(Page, newState, false);
			}
		}
	}
}

