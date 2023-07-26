using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests.Generator;

[TestClass]
[RunsOnUIThread]
public partial class Given_VMWithCommands
{
	public partial class When_ParameterFeed_Then_SubscribedWithSameContext_ViewModel
	{
		public int FeedInvokeCount { get; private set; }

		public int CommandLastParameter { get; private set; } = -1;

		public IFeed<int> MyFeed => Feed.Async(async ct => ++FeedInvokeCount);

		public void DoSomething(int myFeed, CancellationToken ct)
			=> CommandLastParameter = myFeed;
	}

	[TestMethod]
	public async Task When_ParameterFeed_Then_SubscribedWithSameContext()
	{
		var vm = new BindableWhen_ParameterFeed_Then_SubscribedWithSameContext_ViewModel();

		await UIHelper.WaitFor(() => vm.MyFeed != 0, default);

		var current = vm.MyFeed;
		vm.DoSomething.Execute(null);

		await UIHelper.WaitFor(() => vm.Model.CommandLastParameter != -1, default);

		Assert.AreEqual(1, vm.FeedInvokeCount);
		Assert.AreEqual(current, vm.Model.CommandLastParameter);
	}

	[TestMethod]
	public async Task When_ParameterFeed_Then_SubscribedWithSameContext_UsingUI()
	{
		FeedView view;
		Button doSomething;
		var vm = new BindableWhen_ParameterFeed_Then_SubscribedWithSameContext_ViewModel();
		var ui = new StackPanel
		{
			DataContext = vm,
			Children =
			{
				(view = new FeedView()),
				(doSomething = new Button())
			}
		};

		view.SetBinding(FeedView.SourceProperty, new Binding { Path = new PropertyPath("MyFeed") });
		doSomething.SetBinding(Button.CommandProperty, new Binding { Path = new PropertyPath("DoSomething") });

		await UIHelper.Load(ui, default);

		await UIHelper.WaitFor(() => vm.MyFeed is not 0, default);

		var current = vm.MyFeed;
		doSomething.Command.Execute(null);

		await UIHelper.WaitFor(() => vm.Model.CommandLastParameter != -1, default);

		Assert.AreEqual(1, vm.FeedInvokeCount);
		Assert.AreEqual(current, vm.Model.CommandLastParameter);
	}
}
