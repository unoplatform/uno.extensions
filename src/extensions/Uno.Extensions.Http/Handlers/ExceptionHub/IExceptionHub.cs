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
#pragma warning disable CA1003 // Use generic event handler instances
        event EventHandler<Exception> OnExceptionReported;
#pragma warning restore CA1003 // Use generic event handler instances

        /// <summary>
        /// Reports the exception <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/>.</param>
        void ReportException(Exception exception);
    }
}
