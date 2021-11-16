using System;
using System.Collections.Generic;
using System.Text;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels
{
	class CartViewModel
	{
		private readonly ICartService _cartService;
		public CartViewModel(ICartService cartService)
		{
			_cartService = cartService;
		}

		public IFeed<Cart> Cart => Feed.Async<Cart>(async ct => await _cartService.Get(ct));
	}
}
