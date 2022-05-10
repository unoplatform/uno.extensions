using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public partial class Given_Methods_Then_GenerateCommands : FeedUITests
{
	public partial class When_SyncParameterLess_Void_ViewModel
	{
		public int InvokeCount { get; set; }

		public void MyMethod()
			=> InvokeCount++;
	}

	[TestMethod]
	public async Task When_SyncParameterLess_Void()
	{
		var vm = new When_SyncParameterLess_Void_ViewModel.BindableWhen_SyncParameterLess_Void_ViewModel();

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_SyncOneParameter_Void_ViewModel
	{
		public int InvokeCount { get; set; }

		public string? LastInvokeParameter { get; set; }

		public void MyMethod(string parameter)
		{
			LastInvokeParameter = parameter;
			InvokeCount++;
		}
	}

	[TestMethod]
	public async Task When_SyncOneParameter_Void()
	{
		var vm = new When_SyncOneParameter_Void_ViewModel.BindableWhen_SyncOneParameter_Void_ViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_TaskParameterLess_Void_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public async Task MyMethod()
		{
			EndExecution();
			InvokeCount++;
			_execution = new();
			await _execution.Task;
			EndedCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_TaskParameterLess_Void()
	{
		var vm = new When_TaskParameterLess_Void_ViewModel.BindableWhen_TaskParameterLess_Void_ViewModel();

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);
		
		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_TaskOneParameter_Void_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public string? LastInvokeParameter { get; set; }

		public async Task MyMethod(string parameter)
		{
			EndExecution();
			LastInvokeParameter = parameter;
			InvokeCount++;
			_execution = new();
			await _execution.Task;
			EndedCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_TaskOneParameter_Void()
	{
		var vm = new When_TaskOneParameter_Void_ViewModel.BindableWhen_TaskOneParameter_Void_ViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);
		
		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_ValueTaskParameterLess_Void_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public async ValueTask MyMethod()
		{
			EndExecution();
			InvokeCount++;
			_execution = new();
			await _execution.Task;
			EndedCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_ValueTaskParameterLess_Void()
	{
		var vm = new When_ValueTaskParameterLess_Void_ViewModel.BindableWhen_ValueTaskParameterLess_Void_ViewModel();

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_ValueTaskOneParameter_Void_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public string? LastInvokeParameter { get; set; }

		public async ValueTask MyMethod(string parameter)
		{
			EndExecution();
			LastInvokeParameter = parameter;
			InvokeCount++;
			_execution = new();
			await _execution.Task;
			EndedCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_ValueTaskOneParameter_Void()
	{
		var vm = new When_ValueTaskOneParameter_Void_ViewModel.BindableWhen_ValueTaskOneParameter_Void_ViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}


	private async ValueTask WaitFor(Func<bool> predicate)
	{
		await Task.Yield();
		await Task.Yield();
		await Task.Yield();

		for (var i = 0; !predicate() && i < 100; i++)
		{
			await Task.Delay(1, CT);
		}
	}
}
