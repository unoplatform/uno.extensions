using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Commands;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Commands;

[TestClass]
public class Given_AsyncCommand : FeedUITests
{
	[TestMethod]
	public void When_Execute_Then_ReportStartSync()
	{
		var config = new CommandConfig { Execute = async (p, ct) => { } };
		var (sut, executions) = Create(config);
		var parameter = new object();

		sut.Execute(parameter);
		executions.Count.Should().Be(1);
		executions.Single().Parameter.Should().Be(parameter);
	}

	[TestMethod]
	public async Task When_ExecuteThrow_Then_EndReportException()
	{
		var error = new TestException();
		var config = new CommandConfig { Execute = async (p, ct) => throw error };
		var (sut, executions) = Create(config);

		sut.Execute(null);
		await executions.Single().Wait();

		executions.Single().End!.Error.Should().BeSameAs(error);
	}

	[TestMethod]
	public async Task When_ExecuteMultipleConcurrentWithSameParameter_Then_ExecuteConcurrentAndReportMultipleExecutionStartAndEnd()
	{
		var tasks = new List<TaskCompletionSource>();
		var config = new CommandConfig { Execute = async (p, ct) =>
		{
			var task = new TaskCompletionSource();
			tasks.Add(task);
			await task.Task;
		} };
		var (sut, executions) = Create(config);
		var parameter = new object();

		sut.Execute(parameter);
		executions.Count.Should().Be(1); // Start should be reported synchronously

		await WaitFor(() => tasks.Count > 0); // Wait for the executions to effectively run in order to have the 'tasks' ordered properly!

		sut.Execute(parameter);
		executions.Count.Should().Be(2); // Start should be reported synchronously

		await WaitFor(() => tasks.Count > 1); // Wait for the 2 executions to effectively run concurrently. 

		tasks[0].SetResult();
		await executions[0].Wait(100);

		tasks[1].SetResult();
		await executions[1].Wait();
	}

	[TestMethod]
	public async Task When_ExecuteMultipleConcurrentWithDifferentParameters_Then_ExecuteConcurrentAndReportMultipleExecutionStartAndEnd()
	{
		var tasks = new List<TaskCompletionSource>();
		var config = new CommandConfig
		{
			Execute = async (p, ct) =>
			{
				var task = new TaskCompletionSource();
				tasks.Add(task);
				await task.Task;
			}
		};
		var (sut, executions) = Create(config);

		sut.Execute(new object());
		executions.Count.Should().Be(1); // Start should be reported synchronously

		await WaitFor(() => tasks.Count > 0); // Wait for the executions to effectively run in order to have the 'tasks' ordered properly!

		sut.Execute(new object());
		executions.Count.Should().Be(2); // Start should be reported synchronously

		await WaitFor(() => tasks.Count > 1); // Wait for the 2 executions to effectively run concurrently. 

		tasks[0].SetResult();
		await executions[0].Wait(100);

		tasks[1].SetResult();
		await executions[1].Wait();
	}

	[TestMethod]
	public async Task When_DisposeWhileExecuting_Then_ExecutionEnded()
	{
		var tasks = new List<TaskCompletionSource>();
		var config = new CommandConfig
		{
			Execute = async (p, ct) =>
			{
				var task = new TaskCompletionSource();
				using var _ = ct.Register(task.SetCanceled);
				tasks.Add(task);
				await task.Task;
			}
		};
		var (sut, executions) = Create(config);

		sut.Execute(null);
		sut.Dispose();

		await executions[0].Wait();
	}

	[TestMethod]
	public async Task When_ProvidingParameter_Then_ParameterNotInvokedOnUIThread()
	{
		var isOnUIThread = new TaskCompletionSource<bool>();
		async IAsyncEnumerable<IMessage> Parameter(SourceContext ctx)
		{
			isOnUIThread.TrySetResult(DispatcherHelper.HasThreadAccess);
			yield break;
		}
		var config = new CommandConfig
		{
			Parameter = Parameter,
			Execute = async (p, ct) => { }
		};
		var (sut, executions) = Create(config);

		await ExecuteOnDispatcher(() => sut.Execute(null));

		(await isOnUIThread.Task).Should().BeFalse();
	}

	private async Task WaitFor(Func<bool> predicate)
	{
		for (var i = 0; i < 100; i++)
		{
			if (predicate())
			{
				return;
			}

			await Task.Delay(1);
		}

		throw new TimeoutException();
	}

	private (AsyncCommand sut, List<Execution> executions) Create(CommandConfig config)
	{
		var sut = new AsyncCommand("sut", config, ExceptionDispatchInfo.Throw, Context);
		var executions = new List<Execution>();
		sut.ExecutionStarted += (snd, args) => executions.Add(new Execution(args));
		sut.ExecutionCompleted += (snd, args) => executions.Single(e => e.Id == args.Id).ReportCompleted(args);

		return (sut, executions);
	}

	[ImplicitKeys(IsEnabled = false)]
	private record Execution(ExecutionStartedEventArgs Start)
	{
		public const int DefaultTimeoutMs = 1000;

		private readonly TaskCompletionSource<ExecutionCompletedEventArgs> _asyncEnd = new();
		private ExecutionCompletedEventArgs? _end;

		public Guid Id => Start.Id;

		public object? Parameter => Start.Parameter;

		public ExecutionCompletedEventArgs? End => _end;

		public void ReportCompleted(ExecutionCompletedEventArgs args)
		{
			if (args.Id != Id)
			{
				throw new ArgumentOutOfRangeException(nameof(args), "Invalid execution Id");
			}
			if (!object.ReferenceEquals(args.Parameter, Parameter))
			{
				throw new ArgumentOutOfRangeException(nameof(args), "Invalid parameter");
			}

			if (Interlocked.CompareExchange(ref _end, args, null) != null)
			{
				throw new InvalidOperationException("Execution has been completed twice");
			}

			_end = args;
			_asyncEnd.SetResult(args);
		}

		public async Task<ExecutionCompletedEventArgs> Wait(int timeout = DefaultTimeoutMs, CancellationToken ct = default)
		{
			if (Debugger.IsAttached)
			{
				timeout *= 1000;
			}

			var timeoutTask = Task.Delay(timeout, ct);
			await Task.WhenAny(_asyncEnd.Task, timeoutTask);
			if (timeoutTask.IsCompleted)
			{
				throw new TimeoutException();
			}

			return End!;
		}
	}
}
