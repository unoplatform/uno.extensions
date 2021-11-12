using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

public delegate ValueTask<TResult> FuncAsync<TResult>(CancellationToken ct);
public delegate ValueTask<TResult> FuncAsync<in T, TResult>(T arg, CancellationToken ct);
public delegate ValueTask<TResult> FuncAsync<in T1, in T2, TResult>(T1 arg1, T2 arg2, CancellationToken ct);
