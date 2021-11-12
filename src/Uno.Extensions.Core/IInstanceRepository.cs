using System;
using System.Collections.Generic;

namespace Uno.Extensions;

public interface IInstanceRepository
{
    IDictionary<Type, object> Instances { get; }
}
