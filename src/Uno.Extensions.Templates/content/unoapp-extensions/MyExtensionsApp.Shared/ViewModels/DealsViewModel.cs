using System;
using MyExtensionsApp.Models;
using MyExtensionsApp.Services;
using Uno.Extensions.Reactive;

namespace MyExtensionsApp.ViewModels;

public record DealsViewModel(IDealService DealService)
{
	public IFeed<Product[]> Items => Feed.Async(DealService.GetDeals);
}
