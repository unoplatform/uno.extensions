
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions;

/// <summary>
/// Encapsulates an asynchronous method that has no parameters and does not return a value.
/// </summary>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction(CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has one parameter and does not return a value.
/// </summary>
/// <typeparam name="T">
/// The type of the parameter of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg">The parameter of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T>(T arg, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 2 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2>(T1 arg1, T2 arg2, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 3 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 4 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 5 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 6 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 7 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 8 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 9 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 10 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T10">
/// The type of the parameter #10 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="arg10">The parameter #10 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 11 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T10">
/// The type of the parameter #10 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T11">
/// The type of the parameter #11 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="arg10">The parameter #10 of the method that this delegate encapsulates.</param>
/// <param name="arg11">The parameter #11 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 12 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T10">
/// The type of the parameter #10 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T11">
/// The type of the parameter #11 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T12">
/// The type of the parameter #12 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="arg10">The parameter #10 of the method that this delegate encapsulates.</param>
/// <param name="arg11">The parameter #11 of the method that this delegate encapsulates.</param>
/// <param name="arg12">The parameter #12 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 13 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T10">
/// The type of the parameter #10 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T11">
/// The type of the parameter #11 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T12">
/// The type of the parameter #12 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T13">
/// The type of the parameter #13 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="arg10">The parameter #10 of the method that this delegate encapsulates.</param>
/// <param name="arg11">The parameter #11 of the method that this delegate encapsulates.</param>
/// <param name="arg12">The parameter #12 of the method that this delegate encapsulates.</param>
/// <param name="arg13">The parameter #13 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 14 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T10">
/// The type of the parameter #10 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T11">
/// The type of the parameter #11 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T12">
/// The type of the parameter #12 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T13">
/// The type of the parameter #13 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T14">
/// The type of the parameter #14 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="arg10">The parameter #10 of the method that this delegate encapsulates.</param>
/// <param name="arg11">The parameter #11 of the method that this delegate encapsulates.</param>
/// <param name="arg12">The parameter #12 of the method that this delegate encapsulates.</param>
/// <param name="arg13">The parameter #13 of the method that this delegate encapsulates.</param>
/// <param name="arg14">The parameter #14 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 15 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T10">
/// The type of the parameter #10 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T11">
/// The type of the parameter #11 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T12">
/// The type of the parameter #12 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T13">
/// The type of the parameter #13 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T14">
/// The type of the parameter #14 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T15">
/// The type of the parameter #15 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="arg10">The parameter #10 of the method that this delegate encapsulates.</param>
/// <param name="arg11">The parameter #11 of the method that this delegate encapsulates.</param>
/// <param name="arg12">The parameter #12 of the method that this delegate encapsulates.</param>
/// <param name="arg13">The parameter #13 of the method that this delegate encapsulates.</param>
/// <param name="arg14">The parameter #14 of the method that this delegate encapsulates.</param>
/// <param name="arg15">The parameter #15 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, CancellationToken ct);

/// <summary>
/// Encapsulates an asynchronous method that has 16 parameters and does not return a value.
/// </summary>
/// <typeparam name="T1">
/// The type of the parameter #1 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T2">
/// The type of the parameter #2 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T3">
/// The type of the parameter #3 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T4">
/// The type of the parameter #4 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T5">
/// The type of the parameter #5 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T6">
/// The type of the parameter #6 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T7">
/// The type of the parameter #7 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T8">
/// The type of the parameter #8 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T9">
/// The type of the parameter #9 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T10">
/// The type of the parameter #10 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T11">
/// The type of the parameter #11 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T12">
/// The type of the parameter #12 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T13">
/// The type of the parameter #13 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T14">
/// The type of the parameter #14 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T15">
/// The type of the parameter #15 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <typeparam name="T16">
/// The type of the parameter #16 of the method that this delegate encapsulates.
/// This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived.
/// </typeparam>
/// <param name="arg1">The parameter #1 of the method that this delegate encapsulates.</param>
/// <param name="arg2">The parameter #2 of the method that this delegate encapsulates.</param>
/// <param name="arg3">The parameter #3 of the method that this delegate encapsulates.</param>
/// <param name="arg4">The parameter #4 of the method that this delegate encapsulates.</param>
/// <param name="arg5">The parameter #5 of the method that this delegate encapsulates.</param>
/// <param name="arg6">The parameter #6 of the method that this delegate encapsulates.</param>
/// <param name="arg7">The parameter #7 of the method that this delegate encapsulates.</param>
/// <param name="arg8">The parameter #8 of the method that this delegate encapsulates.</param>
/// <param name="arg9">The parameter #9 of the method that this delegate encapsulates.</param>
/// <param name="arg10">The parameter #10 of the method that this delegate encapsulates.</param>
/// <param name="arg11">The parameter #11 of the method that this delegate encapsulates.</param>
/// <param name="arg12">The parameter #12 of the method that this delegate encapsulates.</param>
/// <param name="arg13">The parameter #13 of the method that this delegate encapsulates.</param>
/// <param name="arg14">The parameter #14 of the method that this delegate encapsulates.</param>
/// <param name="arg15">The parameter #15 of the method that this delegate encapsulates.</param>
/// <param name="arg16">The parameter #16 of the method that this delegate encapsulates.</param>
/// <param name="ct">A cancellation to cancel the async operation.</param>
public delegate ValueTask AsyncAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, CancellationToken ct);
