using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Playground.Models;

namespace Playground.ViewModels
{
    public class HomeViewModel
    {
		public string Platform { get; }

		public HomeViewModel(IOptions<AppInfo> appInfo)
		{
			Platform = appInfo.Value.Platform;
		}
	}
}
