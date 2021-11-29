using System;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public record DealsViewModel(IDealService DealService)
{
	public IFeed<Product[]> Items => Feed.Async(DealService.GetDeals);
}
