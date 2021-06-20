using System;
using System.Collections.Generic;

namespace Uno.Extensions.Navigation
{
    public interface IRouteRedirection
    {
        Func<string[], string, IDictionary<string, object>, string> Redirection { get; }
    }
}
