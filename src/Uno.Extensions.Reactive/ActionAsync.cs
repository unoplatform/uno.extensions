using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

public delegate ValueTask ActionAsync(CancellationToken ct);
public delegate ValueTask ActionAsync<in T>(T arg, CancellationToken ct);
