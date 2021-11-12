using System;
using System.Collections.Generic;

namespace Uno.Extensions;

public class InstanceRepository : IInstanceRepository
{
    public IDictionary<Type, object> Instances { get; } = new Dictionary<Type, object>();
}
