using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class TestError
	{
		public TestError()
		{

		}

		public TestError(int code, string message)
		{
			Code = code;
			Message = message;
		}

		public int Code { get; set; }

		public string Message { get; set; }		
	}
}
