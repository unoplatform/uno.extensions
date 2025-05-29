using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Uno.Extensions.Maui.Extensibility;
using Uno.Foundation.Extensibility;

namespace Uno.Extensions.Maui.Platform
{
	internal static partial class MauiInitHelper
	{
		private static IMauiInitExtension? _mauiInitExtension;

		internal static void Initialize(IApplication iApp)
		{
			GetExtension().Initialize(iApp);
		}

		private static IMauiInitExtension GetExtension()
		{
			if (_mauiInitExtension is null)
			{
				ApiExtensibility.CreateInstance(typeof(MauiInitHelper), out _mauiInitExtension);
			}

			return _mauiInitExtension ?? new MauiInitExtension();
		}
	}
}
