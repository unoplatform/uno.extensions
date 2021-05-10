using System;

namespace ApplicationTemplate.Presentation
{
	public class OnboardingItemViewModel
	{
		public OnboardingItemViewModel(string primaryText, string imageUrl)
		{
			PrimaryText = primaryText;
			ImageUrl = imageUrl;
		}

		public string PrimaryText { get; }

		public string ImageUrl { get; }
	}
}
