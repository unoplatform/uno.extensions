using System;
using System.Collections.Generic;

namespace Uno.Extensions;

public class InstanceRepository : IInstanceRepository, ISingletonInstanceRepository, IScopedInstanceRepository, ITransientInstanceRepository
{
    public IDictionary<Type, object> Instances { get; } = new Dictionary<Type, object>();
}
