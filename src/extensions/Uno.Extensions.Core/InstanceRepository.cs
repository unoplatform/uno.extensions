using System.Collections.Generic;
using System;

namespace Uno.Extensions;

public class InstanceRepository : IInstanceRepository
{
    public IDictionary<Type, object> Instances { get; } = new Dictionary<Type, object>();
}
