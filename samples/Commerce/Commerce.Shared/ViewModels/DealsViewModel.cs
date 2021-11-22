using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public class DealsViewModel
{
	private readonly IDealService _dealService;


    public DealsViewModel(IDealService dealService)
    {
		_dealService = dealService;
	}

	public IFeed<Product[]> Items => Feed.Async(_dealService.GetDeals);
}
