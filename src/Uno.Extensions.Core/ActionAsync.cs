using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions;

public delegate ValueTask ActionAsync(CancellationToken ct);
public delegate ValueTask ActionAsync<in T>(T arg, CancellationToken ct);
public delegate ValueTask ActionAsync<in TArg1, in TArg2>(TArg1 arg1, TArg2 arg2, CancellationToken ct);
