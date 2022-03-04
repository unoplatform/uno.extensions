using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers.Queries;

namespace Uno.Gallery.UITests
{
	public class Given_MainPage : TestBase
	{
		[Test]
		public void When_SmokeTest()
		{
			App.WaitForElement(q => q.Marked("UserName"), timeout: TimeSpan.FromSeconds(10));

			App.EnterText("UserName", "test@test.com");
			App.EnterText("Password", "passwordpassword");

			App.Tap("Login");

			App.WaitForElement(q => q.Marked("Filters"), timeout: TimeSpan.FromSeconds(10));
		}
	}
}
