using System;
using System.Collections.Generic;
using System.Text;
using ApplicationTemplate.Business;
using Chinook.DynamicMvvm;

namespace ApplicationTemplate.Presentation
{
	public class ChuckNorrisItemViewModel : ViewModel
	{
		public ChuckNorrisItemViewModel(IViewModel parent, ChuckNorrisQuote quote)
		{
			Parent = parent;
			Quote = quote;
		}

		public IViewModel Parent { get; }

		public ChuckNorrisQuote Quote { get; }

		public bool IsFavorite
		{
			get => this.Get(initialValue: Quote.IsFavorite);
			set => this.Set(value);
		}
	}
}
