using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationTemplate.Presentation
{
	public class WebViewPageViewModel : ViewModel
	{
		public WebViewPageViewModel(string title, Uri sourceUri)
		{
			Title = title;
			SourceUri = sourceUri;
		}

		public string Title { get; set; }

		public Uri SourceUri { get; set; }
	}
}
