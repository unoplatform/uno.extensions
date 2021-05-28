using System;
using System.Collections.Generic;

namespace Uno.Extensions.Navigation
{
    public interface IRouteDefinitions
    {
        IReadOnlyDictionary<string, (Type, Type)> Routes { get; }
    }
}
