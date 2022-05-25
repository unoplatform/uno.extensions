using System;
using System.Collections.Generic;
using System.Text;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial record CartViewModel(ICartService CartService)
{
	public IFeed<Cart> Cart => Feed.Async(CartService.Get);
}
