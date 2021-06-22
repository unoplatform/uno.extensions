using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Http.Handlers
{
	public class ExceptionHub : IExceptionHub
	{
		/// <inheritdoc />
		public event EventHandler<Exception> OnExceptionReported;

		/// <inheritdoc />
		public void ReportException(Exception e)
		{
			OnExceptionReported?.Invoke(this, e);
		}
	}
}
