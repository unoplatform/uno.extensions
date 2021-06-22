using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Http.Handlers
{
	public interface IExceptionHub
	{
		/// <summary>
		/// Occurs when an excepton was reported.
		/// </summary>
		event EventHandler<Exception> OnExceptionReported;

		/// <summary>
		/// Reports the exception <paramref name="exception"/>.
		/// </summary>
		/// <param name="exception"><see cref="Exception"/></param>
		void ReportException(Exception exception);
	}
}
