using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
public class Given_FeedView
{
	[TestMethod]
	public void Bla()
	{
		var sut = new FeedView();


		UIHelper.Content = sut;
	}
}
