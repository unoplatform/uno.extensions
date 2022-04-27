using System;
using System.Collections.Generic;

namespace Uno.Extensions;

internal interface IInstanceRepository
{
    IDictionary<Type, object> Instances { get; }
}

internal interface IScopedInstanceRepository : IInstanceRepository
{
}

internal interface ISingletonInstanceRepository : IInstanceRepository
{
}
