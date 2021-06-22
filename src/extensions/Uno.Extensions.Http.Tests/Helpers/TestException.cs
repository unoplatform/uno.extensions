using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class TestException : Exception
	{
		public TestException(TestError error = null)
		{
			Error = error;
		}

		public TestError Error { get; }
	}
}
