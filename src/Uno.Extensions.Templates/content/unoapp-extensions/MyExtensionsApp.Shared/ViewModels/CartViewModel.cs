using System;
using System.Collections.Generic;
using System.Text;
using MyExtensionsApp.Models;
using MyExtensionsApp.Services;
using Uno.Extensions.Reactive;

namespace MyExtensionsApp.ViewModels;

public partial record CartViewModel(ICartService CartService)
{
	public IFeed<Cart> Cart => Feed.Async(CartService.Get);
}
