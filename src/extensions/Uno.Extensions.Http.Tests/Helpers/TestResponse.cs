using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class TestResponse
	{
		public TestResponse()
		{

		}

		public TestResponse(TestData data, TestError error)
		{
			Data = data;
			Error = error;
		}

		public TestData Data { get; set; }

		public TestError Error { get; set; }
	}
}
