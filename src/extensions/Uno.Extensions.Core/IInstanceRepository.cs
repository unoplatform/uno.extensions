using System.Collections.Generic;
using System;

namespace Uno.Extensions;

public interface IInstanceRepository
{
    IDictionary<Type, object> Instances { get; }
}
