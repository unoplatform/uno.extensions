using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public partial class Given_Methods_Then_GenerateCommands : FeedUITests
{
	public partial class When_ParameterLess_Void_ViewModel
	{
		public int InvokeCount { get; set; }

		public void MyMethod()
			=> InvokeCount++;
	}

	[TestMethod]
	public async Task When_ParameterLess_Void()
	{
		await using var vm = new When_ParameterLess_Void_ViewViewModel();

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_OneParameter_Void_ViewModel
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
	public async Task When_OneParameter_Void()
	{
		await using var vm = new When_OneParameter_Void_ViewViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_OneValueTypeParameter_Void_ViewModel
	{
		public int InvokeCount { get; set; }

		public DateTimeOffset? LastInvokeParameter { get; set; }

		public void MyMethod(DateTimeOffset parameter)
		{
			LastInvokeParameter = parameter;
			InvokeCount++;
		}
	}

	[TestMethod]
	public async Task When_OneValueTypeParameter_Void()
	{
		await using var vm = new When_OneValueTypeParameter_Void_ViewViewModel();

		vm.MyMethod.Execute(new DateTimeOffset(1983, 9, 9, 15, 00, 00, TimeSpan.FromHours(1)));
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be(new DateTimeOffset(1983, 9, 9, 15, 00, 00, TimeSpan.FromHours(1)));
		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_OneNullableValueTypeParameter_Void_ViewModel
	{
		public int InvokeCount { get; set; }

		public DateTimeOffset? LastInvokeParameter { get; set; }

		public void MyMethod(DateTimeOffset? parameter)
		{
			LastInvokeParameter = parameter;
			InvokeCount++;
		}
	}

	[TestMethod]
	public async Task When_OneNullableValueTypeParameter_Void()
	{
		await using var vm = new When_OneNullableValueTypeParameter_Void_ViewViewModel();

		vm.MyMethod.Execute(new DateTimeOffset(1983, 9, 9, 15, 00, 00, TimeSpan.FromHours(1)));
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be(new DateTimeOffset(1983, 9, 9, 15, 00, 00, TimeSpan.FromHours(1)));
		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_OneParameterAndCT_Void_ViewModel
	{
		public int InvokeCount { get; set; }

		public string? LastInvokeParameter { get; set; }

		public void MyMethod(string parameter, CancellationToken ct)
		{
			LastInvokeParameter = parameter;
			InvokeCount++;
		}
	}

	[TestMethod]
	public async Task When_OneParameterAndCT_Void()
	{
		await using var vm = new When_OneParameterAndCT_Void_ViewViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_ParameterLess_Task_ViewModel
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
	public async Task When_ParameterLess_Task()
	{
		await using var vm = new When_ParameterLess_Task_ViewViewModel();

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);
		
		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_OneParameter_Task_ViewModel
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
	public async Task When_OneParameter_Task()
	{
		await using var vm = new When_OneParameter_Task_ViewViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);
		
		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_ParameterLess_ValueTask_ViewModel
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
	public async Task When_ParameterLess_ValueTask()
	{
		await using var vm = new When_ParameterLess_ValueTask_ViewViewModel();

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_OneParameter_ValueTask_ViewModel
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
	public async Task When_OneParameter_ValueTask()
	{
		await using var vm = new When_OneParameter_ValueTask_ViewViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_OneParameterAndCT_ValueTask_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public string? LastInvokeParameter { get; set; }

		public async ValueTask MyMethod(string parameter, CancellationToken ct)
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
	public async Task When_OneParameterAndCT_ValueTask()
	{
		await using var vm = new When_OneParameterAndCT_ValueTask_ViewViewModel();

		vm.MyMethod.Execute("42");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_OneFeedParameter_Void_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public IFeed<string> MyParameter => Feed.Async(async ct => "42");

		public int InvokeCount { get; set; }
		public string? LastInvokeParameter { get; set; }

		public void MyMethod(string myParameter)
		{
			LastInvokeParameter = myParameter;
			InvokeCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_OneFeedParameter_Void_WithoutCommandParameter()
	{
		await using var vm = new When_OneFeedParameter_Void_ViewViewModel();

		// We have to wait for the external parameter to be provided by the feed
		await WaitFor(() => vm.MyMethod.CanExecute(null));

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);
		 
		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_OneFeedParameter_Void_WithCommandParameter()
	{
		await using var vm = new When_OneFeedParameter_Void_ViewViewModel();

		vm.MyMethod.Execute("43");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("43");
		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_OneFeedParameterAndCT_Void_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public IFeed<string> MyParameter => Feed.Async(async ct => "42");

		public int InvokeCount { get; set; }
		public string? LastInvokeParameter { get; set; }

		public void MyMethod(string myParameter, CancellationToken ct)
		{
			LastInvokeParameter = myParameter;
			InvokeCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_OneFeedParameterAndCT_Void_WithoutCommandParameter()
	{
		await using var vm = new When_OneFeedParameterAndCT_Void_ViewViewModel();

		// We have to wait for the external parameter to be provided by the feed
		await WaitFor(() => vm.MyMethod.CanExecute(null));

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_OneFeedParameterAndCT_Void_WithCommandParameter()
	{
		await using var vm = new When_OneFeedParameterAndCT_Void_ViewViewModel();

		vm.MyMethod.Execute("43");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("43");
		vm.InvokeCount.Should().Be(1);
	}

	public partial class When_OneFeedParameter_ValueTask_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public IFeed<string> MyParameter => Feed.Async(async ct => "42");

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public string? LastInvokeParameter { get; set; }

		public async ValueTask MyMethod(string myParameter)
		{
			EndExecution();
			LastInvokeParameter = myParameter;
			InvokeCount++;
			_execution = new();
			await _execution.Task;
			EndedCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_OneFeedParameter_ValueTask_WithoutCommandParameter()
	{
		await using var vm = new When_OneFeedParameter_ValueTask_ViewViewModel();

		// We have to wait for the external parameter to be provided by the feed
		await WaitFor(() => vm.MyMethod.CanExecute(null));

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_OneFeedParameter_ValueTask_WithCommandParameter()
	{
		await using var vm = new When_OneFeedParameter_ValueTask_ViewViewModel();

		vm.MyMethod.Execute("43");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("43");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_OneListFeedParameter_ValueTask_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public IListFeed<string> MyParameter => ListFeed.Async(async ct => ImmutableList.Create("41", "42", "43") as IImmutableList<string>);

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public IImmutableList<string>? LastInvokeParameter { get; set; }

		public async ValueTask MyMethod(IImmutableList<string> myParameter)
		{
			EndExecution();
			LastInvokeParameter = myParameter;
			InvokeCount++;
			_execution = new();
			await _execution.Task;
			EndedCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_OneListFeedParameter_ValueTask_WithoutCommandParameter()
	{
		await using var vm = new When_OneListFeedParameter_ValueTask_ViewViewModel();

		// We have to wait for the external parameter to be provided by the feed
		await WaitFor(() => vm.MyMethod.CanExecute(null));

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().BeEquivalentTo("41", "42", "43");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_OneListFeedParameter_ValueTask_WithCommandParameter()
	{
		await using var vm = new When_OneListFeedParameter_ValueTask_ViewViewModel();

		vm.MyMethod.Execute(ImmutableList.Create("51", "52", "53"));
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().BeEquivalentTo("51", "52", "53");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_OneFeedParameterAndCT_ValueTask_ViewModel
	{
		private TaskCompletionSource _execution = new();

		public IFeed<string> MyParameter => Feed.Async(async ct => "42");

		public int InvokeCount { get; set; }
		public int EndedCount { get; set; }

		public string? LastInvokeParameter { get; set; }

		public async ValueTask MyMethod(string myParameter, CancellationToken ct)
		{
			EndExecution();
			LastInvokeParameter = myParameter;
			InvokeCount++;
			_execution = new();
			await _execution.Task;
			EndedCount++;
		}

		internal void EndExecution()
			=> _execution?.SetResult();
	}

	[TestMethod]
	public async Task When_OneFeedParameterAndCT_ValueTask_WithoutCommandParameter()
	{
		await using var vm = new When_OneFeedParameterAndCT_ValueTask_ViewViewModel();

		// We have to wait for the external parameter to be provided by the feed
		await WaitFor(() => vm.MyMethod.CanExecute(null));

		vm.MyMethod.Execute(null);
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("42");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_OneFeedParameterAndCT_ValueTask_WithCommandParameter()
	{
		await using var vm = new When_OneFeedParameterAndCT_ValueTask_ViewViewModel();

		vm.MyMethod.Execute("43");
		await WaitFor(() => vm.InvokeCount == 1);

		vm.LastInvokeParameter.Should().Be("43");
		vm.InvokeCount.Should().Be(1);
		vm.EndedCount.Should().Be(0);

		vm.Model.EndExecution();
		await WaitFor(() => vm.EndedCount == 1);

		vm.EndedCount.Should().Be(1);
	}

	public partial class When_MultipleFeedParameter_ViewModel
	{
		public IFeed<string> MyParameter => Feed.Async(async ct => nameof(MyParameter));
		public IFeed<string> MyParameter2 => Feed.Async(async ct => nameof(MyParameter2));

		public int InvokeCount { get; set; }
		public (string method, object[] args) LastInvoke { get; set; }

		public void MyMethod(string myParameter, string myParameter2)
		{
			LastInvoke = (nameof(MyMethod), new object[] { myParameter, myParameter2 });
			InvokeCount++;
		}

		public void MyMethodWithCt(string myParameter, string myParameter2, CancellationToken ct)
		{
			LastInvoke = (nameof(MyMethodWithCt), new object[] { myParameter, myParameter2, _ct });
			InvokeCount++;
		}
	}

	[TestMethod]
	[DataRow(nameof(When_MultipleFeedParameter_ViewModel.MyMethod))]
	[DataRow(nameof(When_MultipleFeedParameter_ViewModel.MyMethodWithCt))]
	public async Task When_MultipleFeedParameter_ViewModel_CanExecuteWithoutParameter(string method)
	{
		await using var vm = new When_MultipleFeedParameter_ViewViewModel();

		var commandInfo = vm.GetType().GetMember(method).Single();
		commandInfo.Should().BeAssignableTo<PropertyInfo>(because: "a command should have been generated for that method");

		var command = ((PropertyInfo)commandInfo).GetValue(vm) as ICommand;
		command.Should().NotBeNull();

		// We have to wait for the external parameter to be provided by the feed
		await WaitFor(() => command!.CanExecute(null));

		command!.CanExecute(null).Should().BeTrue();
	}

	[TestMethod]
	[DataRow(nameof(When_MultipleFeedParameter_ViewModel.MyMethod))]
	[DataRow(nameof(When_MultipleFeedParameter_ViewModel.MyMethodWithCt))]
	public async Task When_MultipleFeedParameter_ViewModel_CanExecuteOnlyWithoutParameter(string method)
	{
		await using var vm = new When_MultipleFeedParameter_ViewViewModel();

		var commandInfo = vm.GetType().GetMember(method).Single();
		commandInfo.Should().BeAssignableTo<PropertyInfo>(because: "a command should have been generated for that method");

		var command = ((PropertyInfo)commandInfo).GetValue(vm) as ICommand;
		command.Should().NotBeNull();

		command!.CanExecute(_viewParam).Should().BeFalse();
	}

	[TestMethod]
	[DataRow(nameof(When_MultipleFeedParameter_ViewModel.MyMethod), nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter))]
	[DataRow(nameof(When_MultipleFeedParameter_ViewModel.MyMethodWithCt), nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter), _ct)]
	private async Task When_MultipleFeedParameter_ViewModel_ArgsReDispatchedProperly(string method, params string[] expectedArgs)
	{
		await using var vm = new When_MultipleFeedParameter_ViewViewModel();

		var commandInfo = vm.GetType().GetMember(method).Single();
		commandInfo.Should().BeAssignableTo<PropertyInfo>(because: "a command should have been generated for that method");

		var command = ((PropertyInfo)commandInfo).GetValue(vm) as ICommand;
		command.Should().NotBeNull();

		command!.Execute(null);

		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
		vm.LastInvoke.method.Should().Be(method);
		vm.LastInvoke.args.Should().BeEquivalentTo(expectedArgs);
	}

	public partial class When_MixedViewAndFeedParameter_ViewModel
	{
		public IFeed<string> MyParameter => Feed.Async(async ct => nameof(MyParameter));
		public IFeed<string> MyParameter2 => Feed.Async(async ct => nameof(MyParameter2));

		public int InvokeCount { get; set; }
		public (string method, object[] args) LastInvoke { get; set; }

		public void MyMethod(string theViewParameter, string myParameter)
		{
			LastInvoke = (nameof(MyMethod), new object[] { theViewParameter, myParameter });
			InvokeCount++;
		}

		public void MyMethodWithCt(string theViewParameter, string myParameter, CancellationToken ct)
		{
			LastInvoke = (nameof(MyMethodWithCt), new object[] { theViewParameter, myParameter, _ct });
			InvokeCount++;
		}

		public void MyMethod2(string myParameter, string theViewParameter)
		{
			LastInvoke = (nameof(MyMethod2), new object[] { myParameter, theViewParameter });
			InvokeCount++;
		}

		public void MyMethod2WithCt(string myParameter, string theViewParameter, CancellationToken ct)
		{
			LastInvoke = (nameof(MyMethod2WithCt), new object[] { myParameter, theViewParameter, _ct });
			InvokeCount++;
		}

		public void MyMethod3(string theViewParameter, string myParameter, string myParameter2)
		{
			LastInvoke = (nameof(MyMethod3), new object[] { theViewParameter, myParameter, myParameter2 });
			InvokeCount++;
		}

		public void MyMethod3WithCt(string theViewParameter, string myParameter, string myParameter2, CancellationToken ct)
		{
			LastInvoke = (nameof(MyMethod3WithCt), new object[] { theViewParameter, myParameter, myParameter2, _ct });
			InvokeCount++;
		}
	}

	[TestMethod]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethodWithCt))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod2))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod2WithCt))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod3))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod3WithCt))]
	public async Task When_MixedViewAndFeedParameter_ViewModel_CanExecuteWithParameter(string method)
	{
		await using var vm = new When_MixedViewAndFeedParameter_ViewViewModel();

		var commandInfo = vm.GetType().GetMember(method).Single();
		commandInfo.Should().BeAssignableTo<PropertyInfo>(because: "a command should have been generated for that method");

		var command = ((PropertyInfo)commandInfo).GetValue(vm) as ICommand;
		command.Should().NotBeNull();

		// We have to wait for the external parameter to be provided by the feed
		await WaitFor(() => command!.CanExecute(_viewParam));

		command!.CanExecute(_viewParam).Should().BeTrue();
	}

	[TestMethod]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethodWithCt))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod2))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod2WithCt))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod3))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod3WithCt))]
	public async Task When_MixedViewAndFeedParameter_ViewModel_CanExecuteOnlyWithParameter(string method)
	{
		await using var vm = new When_MixedViewAndFeedParameter_ViewViewModel();

		var commandInfo = vm.GetType().GetMember(method).Single();
		commandInfo.Should().BeAssignableTo<PropertyInfo>(because: "a command should have been generated for that method");

		var command = ((PropertyInfo)commandInfo).GetValue(vm) as ICommand;
		command.Should().NotBeNull();

		command!.CanExecute(null).Should().BeFalse();
	}

	private const string _viewParam = "view_param";
	private const string _ct = "CancellationToken";

	[TestMethod]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod), _viewParam, nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethodWithCt), _viewParam, nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter), _ct)]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod2), nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter), _viewParam)]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod2WithCt), nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter), _viewParam, _ct)]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod3), _viewParam, nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter), nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter2))]
	[DataRow(nameof(When_MixedViewAndFeedParameter_ViewModel.MyMethod3WithCt), _viewParam, nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter), nameof(When_MixedViewAndFeedParameter_ViewModel.MyParameter2), _ct)]
	private async Task When_MixedViewAndFeedParameter_ArgsReDispatchedProperly(string method, params string[] expectedArgs)
	{
		await using var vm = new When_MixedViewAndFeedParameter_ViewViewModel();

		var commandInfo = vm.GetType().GetMember(method).Single();
		commandInfo.Should().BeAssignableTo<PropertyInfo>(because: "a command should have been generated for that method");

		var command = ((PropertyInfo)commandInfo).GetValue(vm) as ICommand;
		command.Should().NotBeNull();

		command!.Execute(_viewParam);

		await WaitFor(() => vm.InvokeCount == 1);

		vm.InvokeCount.Should().Be(1);
		vm.LastInvoke.method.Should().Be(method);
		vm.LastInvoke.args.Should().BeEquivalentTo(expectedArgs);
	}

	[ImplicitCommands(false)]
	public partial class When_ImplicitCommandDisabled_ViewModel
	{
		public void Sync() { }
		public void SyncWithParameter(string myParameter) { }
		public async Task Async() { }
		public async Task AsyncWithParameter(string myParameter) { }
		public async ValueTask Async2() { }
		public async ValueTask AsyncWithParameter2(string myParameter) { }

		[Reactive.Commands.Command]
		public void WithExplicitAttribute() { }
	}

	public partial class When_ImplicitCommandEnabled_ViewModel
	{

		[Reactive.Commands.Command(false)]
		public void WithExplicitAttribute() { }
	}

	[TestMethod]
	public async Task When_ImplicitCommandDisabled_ViewModel_Then_CommandNotGenerated()
	{
		GetMember(nameof(When_ImplicitCommandDisabled_ViewModel.Sync)).Should().NotBeNull().And.BeAssignableTo<MethodInfo>();
		GetMember(nameof(When_ImplicitCommandDisabled_ViewModel.SyncWithParameter)).Should().NotBeNull().And.BeAssignableTo<MethodInfo>();
		GetMember(nameof(When_ImplicitCommandDisabled_ViewModel.Async)).Should().NotBeNull().And.BeAssignableTo<MethodInfo>();
		GetMember(nameof(When_ImplicitCommandDisabled_ViewModel.AsyncWithParameter)).Should().NotBeNull().And.BeAssignableTo<MethodInfo>();
		GetMember(nameof(When_ImplicitCommandDisabled_ViewModel.Async2)).Should().NotBeNull().And.BeAssignableTo<MethodInfo>();
		GetMember(nameof(When_ImplicitCommandDisabled_ViewModel.AsyncWithParameter2)).Should().NotBeNull().And.BeAssignableTo<MethodInfo>();

		MemberInfo GetMember(string name)
			=> typeof(When_ImplicitCommandDisabled_ViewViewModel).GetMember(name, BindingFlags.Instance | BindingFlags.Public).Single();
	}

	[TestMethod]
	public async Task When_ImplicitCommandDisabledAndUseExplicitAttribute_ViewModel_Then_CommandGenerated()
	{
		GetMember(nameof(When_ImplicitCommandDisabled_ViewModel.WithExplicitAttribute))
			.Should().NotBeNull()
			.And.BeAssignableTo<PropertyInfo>()
			.Subject.PropertyType.Should().BeAssignableTo(typeof(ICommand));

		MemberInfo GetMember(string name)
			=> typeof(When_ImplicitCommandDisabled_ViewViewModel).GetMember(name, BindingFlags.Instance | BindingFlags.Public).Single();
	}

	[TestMethod]
	public async Task When_ImplicitCommandEnabledAndUseExplicitAttribute_ViewModel_Then_CommandNotGenerated()
	{
		GetMember(nameof(When_ImplicitCommandEnabled_ViewModel.WithExplicitAttribute)).Should().NotBeNull().And.BeAssignableTo<MethodInfo>();

		MemberInfo GetMember(string name)
			=> typeof(When_ImplicitCommandEnabled_ViewViewModel).GetMember(name, BindingFlags.Instance | BindingFlags.Public).Single();
	}

	[ImplicitFeedCommandParameters(false)]
	public partial class When_ImplicitFeedCommandDisabled_ViewModel
	{
		public IFeed<string> MyParameter { get; } = Feed.Async(async _ => "42");
		public void SyncWithParameter(string myParameter) { }
		public async Task AsyncWithParameter(string myParameter) { }
		public async ValueTask AsyncWithParameter2(string myParameter) { }

		public void WithExplicitAttribute([Reactive.Commands.FeedParameter] string myParameter) { }
		public void WithExplicitAttributeWithName([Reactive.Commands.FeedParameter(nameof(MyParameter))] string anArgument) { }
	}

	[TestMethod]
	public async Task When_ImplicitFeedCommandDisabled_ViewModel_Then_ParameterNotUsed()
	{
		await using var vm = new When_ImplicitFeedCommandDisabled_ViewViewModel();

		var subs = GetSubCommands(vm.SyncWithParameter);
		subs.Should().HaveCount(1);
		HasExternalParameter(subs.First()).Should().BeFalse();

		subs = GetSubCommands(vm.AsyncWithParameter);
		subs.Should().HaveCount(1);
		HasExternalParameter(subs.First()).Should().BeFalse();

		subs = GetSubCommands(vm.AsyncWithParameter2);
		subs.Should().HaveCount(1);
		HasExternalParameter(subs.First()).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_ImplicitFeedCommandDisabledWithExplicitAttribute_ViewModel_Then_ParameterUsed()
	{
		await using var vm = new When_ImplicitFeedCommandDisabled_ViewViewModel();

		// We wait for the feed parameter to be full-filed
		await WaitFor(() => vm.WithExplicitAttribute.CanExecute(null));
		await WaitFor(() => vm.WithExplicitAttributeWithName.CanExecute(null));

		GetSubCommands(vm.WithExplicitAttribute).Any(HasExternalParameter).Should().BeTrue(); 
		GetSubCommands(vm.WithExplicitAttributeWithName).Any(HasExternalParameter).Should().BeTrue();
	}

	public partial class When_UsingConflictingTypesParameter_ViewModel
	{
		public void ConflictWhenNotPrefixedByGlobal1(Uno.System.DateTime parameter) { }

		public void ConflictWhenNotPrefixedByGlobal2(global::System.DateTime parameter) { }
	}

	[TestMethod]
	public async Task When_When_UsingConflictingTypesParameter_Then_Compiles()
	{
		await using var vm = new When_UsingConflictingTypesParameter_ViewViewModel();

		vm.ConflictWhenNotPrefixedByGlobal1.CanExecute(new Uno.System.DateTime()).Should().BeTrue();
		vm.ConflictWhenNotPrefixedByGlobal1.CanExecute(new global::System.DateTime()).Should().BeFalse();
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

	private static object[] GetSubCommands(ICommand command)
			=> ((IEnumerable)((FieldInfo)command
					.GetType()
					.GetMember("_children", BindingFlags.Instance | BindingFlags.NonPublic)
					.Single())
				.GetValue(command)!)
				.Cast<object>()
				.ToArray();

	private static bool HasExternalParameter(object subCommand)
		=> ((FieldInfo)subCommand
				.GetType()
				.GetMember("_externalParameter", BindingFlags.Instance | BindingFlags.NonPublic)
				.Single())
			.GetValue(subCommand) is not null;
}
